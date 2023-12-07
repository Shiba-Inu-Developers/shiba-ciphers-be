FROM mcr.microsoft.com/dotnet/sdk:6.0-bookworm-slim AS build-env

WORKDIR /App

COPY . ./
RUN dotnet restore
RUN dotnet publish -o out

FROM mcr.microsoft.com/dotnet/aspnet:6.0
WORKDIR /App
COPY --from=build-env /App/out .
ENTRYPOINT ["./my-new-app"]

