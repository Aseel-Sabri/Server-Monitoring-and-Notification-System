FROM mcr.microsoft.com/dotnet/runtime:7.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["ServerStatisticsCollection/ServerStatisticsCollection.csproj", "ServerStatisticsCollection/"]
RUN dotnet restore "ServerStatisticsCollection/ServerStatisticsCollection.csproj"
COPY . .
WORKDIR "/src/ServerStatisticsCollection"
RUN dotnet build "ServerStatisticsCollection.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ServerStatisticsCollection.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENV WAIT_VERSION 2.7.2
ADD https://github.com/ufoscout/docker-compose-wait/releases/download/$WAIT_VERSION/wait /wait
RUN chmod +x /wait

ENTRYPOINT /wait && dotnet ServerStatisticsCollection.dll
