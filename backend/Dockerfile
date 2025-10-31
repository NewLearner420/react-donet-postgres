FROM mcr.microsoft.com/dotnet/sdk:8.0

# Install additional development tools
RUN apt-get update && apt-get install -y \
    git \
    curl \
    wget \
    vim \
    postgresql-client \
    && rm -rf /var/lib/apt/lists/*

# Install .NET global tools
RUN dotnet tool install --global dotnet-ef && \
    dotnet tool install --global dotnet-aspnet-codegenerator

ENV PATH="${PATH}:/root/.dotnet/tools"

WORKDIR /app

EXPOSE 8080

# Use a script to restore packages before running
CMD ["sh", "-c", "dotnet restore && dotnet watch run --urls http://0.0.0.0:8080"]