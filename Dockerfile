# ==========================================
# 1. Base Runtime Image
# ==========================================
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

# Cài đặt các gói phụ trợ cho Linux (hỗ trợ Rotativa / wkhtmltopdf & xử lý đồ họa)
RUN apt-get update && apt-get install -y --no-install-recommends \
    wkhtmltopdf \
    libgdiplus \
    fontconfig \
    libxrender1 \
    libxext6 \
    libfreetype6 \
    xfonts-75dpi \
    xfonts-base \
    && rm -rf /var/lib/apt/lists/*

# ==========================================
# 2. Build & Publish Stage
# ==========================================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy file .csproj và restore trước để tận dụng Docker cache layer
COPY ["204Store.csproj", "./"]
RUN dotnet restore "./204Store.csproj"

# Copy toàn bộ mã nguồn và build
COPY . .
RUN dotnet build "204Store.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "204Store.csproj" -c Release -o /app/publish /p:UseAppHost=false

# ==========================================
# 3. Final Production Runtime Image
# ==========================================
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Tạo thư mục và thiết lập symlink cho wkhtmltopdf phục vụ Rotativa trên Linux
RUN mkdir -p /app/wwwroot/Rotativa && \
    ln -sf /usr/bin/wkhtmltopdf /app/wwwroot/Rotativa/wkhtmltopdf && \
    chmod +x /usr/bin/wkhtmltopdf

# Thiết lập biến môi trường mặc định
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "204Store.dll"]
