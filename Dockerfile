FROM node:24.22.1-bookworm-slim AS frontend
WORKDIR /src
COPY PortalTarefas.Frontend/package*.json ./
RUN npm ci
COPY PortalTarefas.Frontend/ ./
RUN npm test && npm run build

FROM mcr.microsoft.com/dotnet/sdk:10.0.401 AS build
WORKDIR /src
COPY . .
RUN dotnet restore PortalTarefas.slnx --locked-mode
RUN dotnet test PortalTarefas.Tests -c Release --no-restore
COPY --from=frontend /src/dist/ PortalTarefas.Web/wwwroot/spa/
RUN dotnet publish PortalTarefas.Web -c Release --no-restore -o /out

FROM mcr.microsoft.com/dotnet/aspnet:10.0.12 AS runtime
WORKDIR /app
COPY --from=build /out/ ./
ENV ASPNETCORE_HTTP_PORTS=8080 Https__Redirecionar=false Database__Inicializar=false
USER $APP_UID
EXPOSE 8080
HEALTHCHECK --interval=30s --timeout=5s --start-period=20s --retries=3 CMD ["dotnet","PortalTarefas.Web.dll","--health-check"]
ENTRYPOINT ["dotnet","PortalTarefas.Web.dll"]
