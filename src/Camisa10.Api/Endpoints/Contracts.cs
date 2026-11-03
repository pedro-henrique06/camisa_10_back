using Camisa10.Domain.Game;

namespace Camisa10.Api.Endpoints;

/// <param name="Club">Id do clube no mundo (de /api/world). O nome também é aceito.</param>
public record NewCareerRequest(string? Name, string Club, string Nation, string Position);

/// <summary>
/// Ação do jogador. <c>Version</c> é a versão da carreira que o app estava mostrando;
/// <c>Power</c> é a força da barra no pênalti (0 a 100).
/// </summary>
public record ActionRequest(ActionType Type, int? Index, int? Version, int? Power = null);

public record CareerSummary(string Id, string Name, int Age, string Club, string Position, string Status, string? FinalTitle, DateTime UpdatedAt);
