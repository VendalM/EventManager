FROM mcr.microsoft.com/dotnet/sdk:8.0 AS restore
ARG PROJECT
WORKDIR /src

COPY ["docker/certs/", "/usr/local/share/ca-certificates/eventmanager/"]
RUN if find /usr/local/share/ca-certificates/eventmanager -name "*.crt" -print -quit | grep -q .; then update-ca-certificates; fi

COPY ["src/Contracts/Contracts.csproj", "Contracts/"]
COPY ["src/Users/Users.csproj", "Users/"]
COPY ["src/Events/Events.csproj", "Events/"]
COPY ["src/Bookings/Bookings.csproj", "Bookings/"]
RUN dotnet restore "$PROJECT"

FROM restore AS development
ARG PROJECT
ENV PROJECT=$PROJECT
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY src/. .
RUN dotnet build "$PROJECT" -c Debug --no-restore
ENTRYPOINT ["sh", "-c", "dotnet run --project \"$PROJECT\" --configuration Debug --no-build --no-launch-profile --urls http://+:8080"]

FROM restore AS publish
ARG PROJECT
COPY src/. .
RUN dotnet publish "$PROJECT" -c Release -o /app/publish /p:UseAppHost=false --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS production
ARG APP_DLL
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
ENV APP_DLL=$APP_DLL
EXPOSE 8080

COPY ["docker/certs/", "/usr/local/share/ca-certificates/eventmanager/"]
RUN if find /usr/local/share/ca-certificates/eventmanager -name "*.crt" -print -quit | grep -q .; then update-ca-certificates; fi
COPY --from=publish /app/publish .
ENTRYPOINT ["sh", "-c", "dotnet \"$APP_DLL\""]
