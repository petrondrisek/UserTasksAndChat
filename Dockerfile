FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY *.sln ./
COPY *.csproj ./
RUN dotnet restore

COPY . ./

RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
EXPOSE 80

ENV ASPNETCORE_ENVIRONMENT=Production

COPY --from=build /app/publish ./
RUN mkdir -p /app/data
COPY database.dat /app/data/

ENTRYPOINT ["dotnet", "UserTasksAndChat.dll"]
