FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5244
EXPOSE 11111
EXPOSE 30000

ENV ASPNETCORE_URLS=http://+:5244

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG configuration=Release
COPY . .
RUN dotnet restore "samples/support-center/src/backend/SupportCenter.csproj"
WORKDIR "/samples/support-center/src/backend"
RUN dotnet build "SupportCenter.csproj" -c $configuration -o /app/build

FROM build AS publish
ARG configuration=Release
RUN dotnet publish "SupportCenter.csproj" -c $configuration -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SupportCenter.dll"]