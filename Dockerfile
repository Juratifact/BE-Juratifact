# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# SỬA LỖI: Bỏ chữ "BackEnd/" vì trên Git các thư mục này nằm ở root
COPY ["Juratifact.API/Juratifact.API.csproj", "Juratifact.API/"]
COPY ["Juratifact.Repository/Juratifact.Repository.csproj", "Juratifact.Repository/"]
COPY ["Juratifact.Service/Juratifact.Service.csproj", "Juratifact.Service/"]

# Chạy restore
RUN dotnet restore "Juratifact.API/Juratifact.API.csproj"

# Copy toàn bộ code
COPY . .

# Build dự án
WORKDIR "/src/Juratifact.API"
RUN dotnet build "Juratifact.API.csproj" -c Release -o /app/build

# Stage 2: Publish
FROM build AS publish
RUN dotnet publish "Juratifact.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 3: Chạy ứng dụng
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Juratifact.API.dll"]