cd ..
docker-compose up -d
ls
cd ./Valuator
dotnet run --urls "http://0.0.0.0:5001" &
dotnet run --urls "http://0.0.0.0:5002" &
dotnet run --urls "http://0.0.0.0:5003" &
dotnet run --urls "http://0.0.0.0:5004" &


