FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .

# Support both repository root context (default in Railway) and /Backend subfolder context
RUN if [ -d "Backend" ]; then \
        dotnet restore Backend/NOTQ.API/NOTQ.API.csproj && \
        dotnet publish Backend/NOTQ.API/NOTQ.API.csproj -c Release -o /app/out; \
    else \
        dotnet restore NOTQ.API/NOTQ.API.csproj && \
        dotnet publish NOTQ.API/NOTQ.API.csproj -c Release -o /app/out; \
    fi

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/out ./
ENV ASPNETCORE_URLS=http://+:$PORT
ENTRYPOINT ["dotnet", "NOTQ.API.dll"]
