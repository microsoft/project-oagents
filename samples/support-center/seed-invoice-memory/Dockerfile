FROM mcr.microsoft.com/dotnet/runtime:7.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["seed-invoice-memory/seed-invoice-memory.csproj", "seed-invoice-memory/"]
RUN dotnet restore "seed-invoice-memory/seed-invoice-memory.csproj"
COPY . .
WORKDIR "/seed-invoice-memory/"
RUN dotnet build "seed-invoice-memory.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "seed-invoice-memory.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "seed-invoice-memory.dll"]
