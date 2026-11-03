using Camisa10.Domain.Game;

namespace Camisa10.Api.Endpoints;

public record NewCareerRequest(string? Name, string Club, string Nation, string Position);

/// <summary>Ação do jogador. <c>Version</c> é a versão da carreira que o app estava mostrando.</summary>
public record ActionRequest(ActionType Type, int? Index, int? Version);

public record CareerSummary(string Id, string Name, int Age, string Club, string Position, string Status, string? FinalTitle, DateTime UpdatedAt);
