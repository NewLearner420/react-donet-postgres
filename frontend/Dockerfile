# ==========================================
# Stage 1: Development (for docker-compose)
# ==========================================
FROM node:20-alpine AS development

WORKDIR /app

# Copy package files
COPY package*.json ./

# Install ALL dependencies (including devDependencies for development)
RUN npm install

# Copy source code
COPY . .

# Expose Vite dev server port
EXPOSE 3000

# Start development server with hot reload
# The --host flag makes Vite listen on all interfaces (required for Docker)
CMD ["npm", "run", "dev", "--", "--host", "0.0.0.0", "--port", "3000"]

# ==========================================
# Stage 2: Build (for production build)
# ==========================================
FROM node:20-alpine AS build

WORKDIR /app

# Copy package files
COPY package*.json ./
RUN npm ci

# Copy source code
COPY . .

# Build the app (without hardcoded URLs)
RUN npm run build

# ==========================================
# Stage 3: Production (nginx)
# ==========================================
FROM nginx:alpine AS production

# Copy built files from build stage
COPY --from=build /app/dist /usr/share/nginx/html

# Copy nginx config
COPY nginx.conf /etc/nginx/conf.d/default.conf

# Copy runtime config script
COPY generate-config.sh /docker-entrypoint.d/40-generate-config.sh
RUN chmod +x /docker-entrypoint.d/40-generate-config.sh

EXPOSE 80

# Nginx will run the config script automatically
CMD ["nginx", "-g", "daemon off;"]