FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore NOTQ.API/NOTQ.API.csproj
RUN dotnet publish NOTQ.API/NOTQ.API.csproj -c Release -o /app/out

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/out ./
ENV ASPNETCORE_URLS=http://+:$PORT
ENTRYPOINT ["dotnet", "NOTQ.API.dll"]
