FROM mcr.microsoft.com/dotnet/sdk:6.0-alpine AS build-env

WORKDIR /App

COPY *.csproj ./
COPY nuget.config ./
RUN dotnet restore

COPY . ./
RUN dotnet publish -o out

FROM mcr.microsoft.com/dotnet/aspnet:6.0-alpine
WORKDIR /App
COPY --from=build-env /App/out .
ENTRYPOINT ["./my-new-app"]

