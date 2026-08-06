FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore UbudKusCoin/UbudKusCoin.csproj
RUN dotnet publish UbudKusCoin/UbudKusCoin.csproj -c Release -o /out --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /out .
ENV DOTNET_ENVIRONMENT=Production
ENTRYPOINT ["dotnet", "UbudKusCoin.dll"]
