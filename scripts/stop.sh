pkill -f "dotnet run --urls .*5001"
pkill -f "dotnet run --urls .*5002"
cd ..
docker-compose stop