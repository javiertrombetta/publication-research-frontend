# syntax=docker/dockerfile:1

# ---------- Build ----------
# SDK 10 rather than 8, even though the app targets net8.0: it is the SDK the team develops
# against, so what CI compiles is what they compiled locally. Publishing still emits net8.0.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy the project file first so `restore` is cached separately from source changes.
COPY ResearchPublicationManagementSystem.csproj .
RUN dotnet restore ResearchPublicationManagementSystem.csproj

COPY . .

# Stamped from the git tag by CI so a running container can say which release it is. Defaults
# to 0.0.0 for a local build, which is honest — an untagged build is not a version.
ARG VERSION=0.0.0
RUN dotnet publish ResearchPublicationManagementSystem.csproj \
    -c Release -o /app/publish --no-restore \
    -p:Version=${VERSION}

# ---------- Runtime ----------
# Matches the project's target framework. The csproj also sets RollForward, so this would run on
# a newer runtime too, but pinning to the declared target keeps the container predictable.
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Non-root. Unlike the API this writes nothing at runtime — no uploads, no local database, no
# log files — so it needs no writable directories of its own.
RUN useradd --uid 1001 --create-home appuser \
    && chown -R appuser:appuser /app

COPY --from=build /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_RUNNING_IN_CONTAINER=true

USER appuser

# The platform injects PORT at runtime and Program.cs binds Kestrel to it. 8080 is only the
# documented default for when it is absent.
EXPOSE 8080

# No Docker-level HEALTHCHECK: the aspnet runtime image ships without curl or wget. The
# container exposes GET /health for a platform to poll from outside instead.

ENTRYPOINT ["dotnet", "ResearchPublicationManagementSystem.dll"]
