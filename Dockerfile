
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src 
COPY test.csproj ./
RUN dotnet restore
COPY program.cs .
COPY . .
RUN dotnet publish -c $BUILD_CONFIGURATION -o /app/publish --no-restore


FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
USER root
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "test.dll"]
