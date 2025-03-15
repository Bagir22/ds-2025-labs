pkill -f "dotnet run --urls .*5001"
pkill -f "dotnet run --urls .*5002"
pkill -f "dotnet run --urls .*5003"
pkill -f "dotnet run --urls .*5004"
cd ..
docker-compose stop