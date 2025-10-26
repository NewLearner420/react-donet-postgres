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

# Add .NET tools to PATH
ENV PATH="${PATH}:/root/.dotnet/tools"

# Set working directory
WORKDIR /workspaces/react-donet-postgres

# Keep container running for development
CMD ["sleep", "infinity"]