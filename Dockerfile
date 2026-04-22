# Stage 1: Build (Sử dụng SDK 8.0 để build code)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# 1. Copy các file .csproj vào đúng thư mục để restore (giúp cache layer nhanh hơn)
COPY ["Juratifact.API/Juratifact.API.csproj", "Juratifact.API/"]
COPY ["Juratifact.Repository/Juratifact.Repository.csproj", "Juratifact.Repository/"]
COPY ["Juratifact.Service/Juratifact.Service.csproj", "Juratifact.Service/"]

# 2. Restore các dependencies
RUN dotnet restore "Juratifact.API/Juratifact.API.csproj"

# 3. Copy toàn bộ mã nguồn còn lại
COPY . .

# 4. Build dự án API
WORKDIR "/src/Juratifact.API"
RUN dotnet build "Juratifact.API.csproj" -c Release -o /app/build

# Stage 2: Publish (Đóng gói ứng dụng thành file chạy)
FROM build AS publish
RUN dotnet publish "Juratifact.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 3: Final (Tạo image gọn nhẹ để chạy trên Azure)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Copy file từ bước publish sang
COPY --from=publish /app/publish .

# Lệnh khởi chạy ứng dụng
ENTRYPOINT ["dotnet", "Juratifact.API.dll"]