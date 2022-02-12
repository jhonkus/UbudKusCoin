rm -rf publish-linux

dotnet publish -c Release -r linux-x64 -o ./publish-linux

cd publish-linux
zip -r a-uksc.zip .

cp a-uksc.zip ~/NODES/node1
cp a-uksc.zip ~/NODES/node2
cp a-uksc.zip ~/NODES/node3
cp a-uksc.zip ~/NODES/node4


unzip -o ~/NODES/node1/a-uksc.zip -d ~/NODES/node1 
unzip -o ~/NODES/node2/a-uksc.zip -d ~/NODES/node2
unzip -o ~/NODES/node3/a-uksc.zip -d ~/NODES/node3
unzip -o ~/NODES/node4/a-uksc.zip -d ~/NODES/node4

cd ..


cd ENV-SAMPLES

cp .env1 ~/NODES/node1/.env
cp .env2 ~/NODES/node2/.env
cp .env3 ~/NODES/node3/.env
cp .env4 ~/NODES/node4/.env



cd ..

