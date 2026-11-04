using System.Collections;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.EntityFrameworkCore.Extensions;

namespace Camisa10.Api.Data;

/// <summary>
/// Completa os documentos gravados por versões antigas do jogo. O provedor do EF para MongoDB recusa
/// abrir um documento sem um campo que o modelo exige ("Document element is missing for required
/// non-nullable property"), então, a cada novo campo, as carreiras antigas quebrariam. Na subida da API,
/// cada documento ganha os campos que faltam com o valor padrão da classe (o mesmo de um objeto novo),
/// inclusive dentro dos subdocumentos e listas (competições, mesas, duelos...).
/// </summary>
public static class DocumentUpgrader
{
    /// <summary>Atualiza todas as coleções e devolve quantos documentos mudaram.</summary>
    public static async Task<int> UpgradeAsync(Camisa10DbContext ctx, IMongoDatabase db, CancellationToken ct)
    {
        var changed = 0;
        foreach (var et in ctx.Model.GetEntityTypes().Where(e => !e.IsOwned()))
        {
            var collection = db.GetCollection<BsonDocument>(et.GetCollectionName());
            using var cursor = await collection.FindAsync(FilterDefinition<BsonDocument>.Empty, cancellationToken: ct);
            while (await cursor.MoveNextAsync(ct))
            {
                foreach (var doc in cursor.Current)
                {
                    if (!Fill(doc, et)) continue;
                    await collection.ReplaceOneAsync(Builders<BsonDocument>.Filter.Eq("_id", doc["_id"]), doc, cancellationToken: ct);
                    changed++;
                }
            }
        }
        return changed;
    }

    /// <summary>Põe no documento os campos que faltam. Devolve se algo mudou.</summary>
    public static bool Fill(BsonDocument doc, IReadOnlyEntityType et)
    {
        var template = New(et.ClrType);
        var changed = false;
        foreach (var p in et.GetProperties())
        {
            if (p.IsShadowProperty() || p.PropertyInfo == null) continue;
            var name = p.GetElementName();
            if (string.IsNullOrEmpty(name) || doc.Contains(name)) continue;
            var value = template == null ? null : p.PropertyInfo.GetValue(template);
            if (value == null)
            {
                if (p.IsNullable) continue;
                value = Default(p.ClrType);
                if (value == null) continue;
            }
            doc[name] = ToBson(value, p);
            changed = true;
        }

        foreach (var n in et.GetNavigations().Where(n => n.TargetEntityType.IsOwned()))
        {
            var target = n.TargetEntityType;
            var name = target.GetContainingElementName();
            if (string.IsNullOrEmpty(name)) continue;
            if (!doc.TryGetValue(name, out var existing) || existing.IsBsonNull)
            {
                if (n.IsCollection)
                {
                    if (existing is { IsBsonNull: true }) continue;
                    doc[name] = new BsonArray();
                    changed = true;
                    continue;
                }
                // subdocumento obrigatório (ex.: Attributes = new()): cria com os valores padrão
                var value = template == null || n.PropertyInfo == null ? null : n.PropertyInfo.GetValue(template);
                if (value == null || existing is { IsBsonNull: true }) continue;
                var sub = new BsonDocument();
                Fill(sub, target);
                doc[name] = sub;
                changed = true;
                continue;
            }
            if (n.IsCollection && existing is BsonArray array)
            {
                foreach (var item in array.OfType<BsonDocument>())
                    changed |= Fill(item, target);
            }
            else if (existing is BsonDocument d)
            {
                changed |= Fill(d, target);
            }
        }
        return changed;
    }

    private static object? New(Type t)
    {
        try
        {
            return Activator.CreateInstance(t);
        }
        catch
        {
            return null;
        }
    }

    private static object? Default(Type t)
    {
        if (t == typeof(string)) return "";
        if (t.IsValueType) return Activator.CreateInstance(t);
        if (t.IsGenericType && typeof(IEnumerable).IsAssignableFrom(t)) return New(t);
        return null;
    }

    private static BsonValue ToBson(object value, IReadOnlyProperty p)
    {
        var converter = p.GetValueConverter() ?? p.FindTypeMapping()?.Converter;
        if (converter != null) value = converter.ConvertToProvider(value) ?? value;
        return value switch
        {
            BsonValue b => b,
            string s => new BsonString(s),
            // enum sem conversão: o provedor grava o número
            Enum e => new BsonInt32(Convert.ToInt32(e)),
            DateTime d => new BsonDateTime(d),
            IEnumerable list => new BsonArray(list.Cast<object?>().Select(x => x == null ? BsonNull.Value : BsonTypeMapper.MapToBsonValue(x is Enum en ? Convert.ToInt32(en) : x))),
            _ => BsonTypeMapper.MapToBsonValue(value)
        };
    }
}
