# rm -rf publish-linux

dotnet publish -c Release -r linux-x64 -o ./publish-linux
#dotnet publish -c Release -r osx-x64 -o ./publish-osx
#dotnet publish -c Release -r win-x64 -o ./publish-win64  

# zip -r uksc.zip publish-linux

mkdir -p ~/NODES
mkdir -p ~/NODES/node1
mkdir -p ~/NODES/node2
mkdir -p ~/NODES/node3
mkdir -p ~/NODES/node4

rm -rf ~/NODES/node1/*
rm -rf ~/NODES/node2/*
rm -rf ~/NODES/node3/*
rm -rf ~/NODES/node4/*

cp publish-linux/* -d ~/NODES/node1 
cp publish-linux/* -d ~/NODES/node2 
cp publish-linux/* -d ~/NODES/node3 
cp publish-linux/* -d ~/NODES/node4 

cp env-examples/.env1 ~/NODES/node1/.env
cp env-examples/.env2 ~/NODES/node2/.env
cp env-examples/.env3 ~/NODES/node3/.env
cp env-examples/.env4 ~/NODES/node4/.env

