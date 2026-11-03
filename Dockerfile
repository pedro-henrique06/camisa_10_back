FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Camisa10.slnx ./
COPY src/Camisa10.Domain/Camisa10.Domain.csproj src/Camisa10.Domain/
COPY src/Camisa10.Api/Camisa10.Api.csproj src/Camisa10.Api/
RUN dotnet restore src/Camisa10.Api/Camisa10.Api.csproj
COPY src/ src/
RUN dotnet publish src/Camisa10.Api/Camisa10.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Camisa10.Api.dll"]
