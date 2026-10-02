FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["./src/CaseAuth.Api/CaseAuth.Api.csproj", "CaseAuth.Api/"]
RUN dotnet restore "CaseAuth.Api/CaseAuth.Api.csproj"
COPY . .
WORKDIR "/src/src/CaseAuth.Api"
RUN dotnet publish "CaseAuth.Api.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "CaseAuth.Api.dll"]
