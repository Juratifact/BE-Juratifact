# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# SỬA TẠI ĐÂY: Loại bỏ tên thư mục cha nếu file .csproj nằm ngay trong các thư mục con ở root
COPY ["Juratifact.API/Juratifact.API.csproj", "Juratifact.API/"]
COPY ["Juratifact.Repository/Juratifact.Repository.csproj", "Juratifact.Repository/"]
COPY ["Juratifact.Service/Juratifact.Service.csproj", "Juratifact.Service/"]

RUN dotnet restore "Juratifact.API/Juratifact.API.csproj"

COPY . .

WORKDIR "/src/Juratifact.API"
RUN dotnet build "Juratifact.API.csproj" -c Release -o /app/build

# Stage 2: Publish
FROM build AS publish
RUN dotnet publish "Juratifact.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 3: Final
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Juratifact.API.dll"]