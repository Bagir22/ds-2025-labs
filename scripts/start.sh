cd ../Valuator/
dotnet run --urls "http://0.0.0.0:5001" &
dotnet run --urls "http://0.0.0.0:5002" &
sleep 5
cd ..
docker-compose up -d
