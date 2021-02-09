#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/runtime:5.0-buster-slim AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:5.0-buster-slim AS build
WORKDIR /src
COPY ["src/Anteater.Intercom.Service/Anteater.Intercom.Service.csproj", "src/Anteater.Intercom.Service/"]
COPY ["src/RtspClientSharp.Decoding/RtspClientSharp.Decoding.csproj", "src/RtspClientSharp.Decoding/"]
RUN dotnet restore "src/Anteater.Intercom.Service/Anteater.Intercom.Service.csproj"
COPY . .
WORKDIR "/src/src/Anteater.Intercom.Service"
RUN dotnet build "Anteater.Intercom.Service.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Anteater.Intercom.Service.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Anteater.Intercom.Service.dll"]
