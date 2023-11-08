FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["MessageProcessing/MessageProcessing.csproj", "MessageProcessing/"]
RUN dotnet restore "MessageProcessing/MessageProcessing.csproj"
COPY . .
WORKDIR "/src/MessageProcessing"
RUN dotnet build "MessageProcessing.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "MessageProcessing.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
#
ENV WAIT_VERSION 2.7.2
ADD https://github.com/ufoscout/docker-compose-wait/releases/download/$WAIT_VERSION/wait /wait
RUN chmod +x /wait

ENTRYPOINT /wait && dotnet MessageProcessing.dll
