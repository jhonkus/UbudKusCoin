# UbudKusCoin
Simple Cryptocurrencies with Proof Of Stake  Consensus Algorithm.

WHAT NEW 23 Feb 2022
- Support Mnemonic 12 words
- Address with base 58
- P2P with Grpc


Articles for This Project:

- Part 1

https://putukusuma.medium.com/developing-simple-crypto-application-using-c-48258c2d4e45

- part 2

https://putukusuma.medium.com/creating-simple-cryptocurrency-using-c-and-net-core-part-2-menu-and-database-4ae842098f55

- Part3

https://putukusuma.medium.com/creating-simple-cryptocurrency-using-c-and-net-core-part-3-wallet-8bbfe0544770

- Part4

https://putukusuma.medium.com/creating-simple-cryptocurrency-using-c-and-net-part-4-block-header-c8ad97fd237b


- Part5
https://putukusuma.medium.com/creating-simple-cryptocurrency-part-5-peer-to-peer-p2p-with-grpc-f96913ddd7dd


Videos:

- https://youtu.be/TYM55x7I8us

- https://youtu.be/gpYKUWGBxf4

- https://youtu.be/tAJKbySs9JY
 



Developed with C# and .Net Core 5.0

This Solution have 3 projects

- UbudKusCoin  is Blockchain Core
- ConsoleWallet is desktop wallet
- BlockExporer is desktop explorer


### Requirement
Net SDK 5.0 https://dotnet.microsoft.com/download/dotnet/5.0

### How Install Net SDK 5.0
- download https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/sdk-5.0.100-linux-x64-binaries
- cd ~/Downloads  (assume the sdk downloaded in Downloads folter)
- mkdir -p $HOME/dotnet && tar zxf dotnet-sdk-5.0.100-linux-x64.tar.gz -C $HOME/dotnet
- export DOTNET_ROOT=$HOME/dotnet
- export PATH=$PATH:$HOME/dotnet

### IDE
- Visual Studio Comunity Edition https://visualstudio.microsoft.com/downloads
- For linux user VSCode, follow instruction in this website  https://code.visualstudio.com/docs/languages/dotnet 


### Instalation

Afer install .Net Core SDK 5.0 and Visual Studio Code, do next step

Clone repository

```
> git clone https://github.com/jhonkus/UbudKusCoin.git

> cd UbudKusCoin

> dotnet restore

```

### Build project to produce binnary files.  

The binnary files can copy to the some folder to easy test P2P and Proof of Stake

build for linux:

```
dotnet publish -c Release -r linux-x64 -o ./publish-linux
```

Build for macosx
```
dotnet publish -c Release -r osx-x64 -o ./publish-osx
```

Build for windows
```
dotnet publish -c Release -r win-x64 -o ./publish-win64  
```

#### Create 4 folder and Copy binnary files to the folder
```
mkdir NODES
cd NODES
mkdir node1
mkdir node2
mkdir node3
mkdir node4
```

#### Copy binnary files to each folders, here i use linux

```
cp publish-linux/* -d ~/NODES/node1 
cp publish-linux/* -d ~/NODES/node2 
cp publish-linux/* -d ~/NODES/node3 
cp publish-linux/* -d ~/NODES/node4 
```

#### Copy .env* files to each folder and rename it to .env
```
cp env-examples/.env1 ~/NODES/node1/.env
cp env-examples/.env2 ~/NODES/node2/.env
cp env-examples/.env3 ~/NODES/node3/.env
cp env-examples/.env4 ~/NODES/node4/.env
```


until here you have 4 folder and each folder hamve .env file
each .env file have diffrent values.


### Run all nodes 

Run node1
```
> cd NODES/node1
> ./UbudKusCoin
```
wait until node1 generate some blocks, let's saya 5 blocks


Run node2
Open new terminal

```
> cd NODES/node2
> ./UbudKusCoin
```

See on the console of node2, node2 will download blocks from node1
both node1 and node2 will do minting and do staking.
and one of them will make blokcs.


Run node3
Open new terminal

```
> cd NODES/node3
> ./UbudKusCoin
```

See on the console of node3, node3 will download blocks from node1
and node2, 
All 3 nodes:  node1, node2 and node3 will do minting and do staking.
and one of them will make blokcs.



Run node4
Open new terminal

```
> cd NODES/node4
> ./UbudKusCoin
```

See on the console of node4, node4 will download blocks from node1, node2 and node3. 
All 4 nodes:  node1, node2, node3 and node4 will do minting and do staking.
and one of them will make blokcs.


Any question please put on github issues.



To run BlockExplorer

```
> cd BlockExplorer
> dotnet run

```

To run ConsoleWallet

```
> cd ConsoleWallet
> dotnet run

```

Restore Genesis Account Console Wallet

- Select menu no 2 restore account
- input this: 37115820268057954843929458901983051845242353300769768346456079873593606626394


## Edit Project

Open Project with Visual Studio Code.


## Build project for Publish

- Net Runtime

```
dotnet publish -c Release -o ./publish-net
```


## Deploy UbudKusCoin on AWS Lightstall or other vps linux

- Connect to linux vps with ssh client 
```
ssh -i ~/SSH/ssh.pem your-user@your-IP     (y~/SSH/ssh.pem is path of your private key)
```

- Download and install .net core sdk5.0 on vps
```
wget https://download.visualstudio.microsoft.com/download/pr/820db713-c9a5-466e-b72a-16f2f5ed00e2/628aa2a75f6aa270e77f4a83b3742fb8/dotnet-sdk-5.0.100-linux-x64.tar.gz

ls   (make sure the file dotnet-sdk-5.0.100-linux-x64.tar.gz exist)

mkdir -p $HOME/dotnet   (create folder dotnet)

tar zxf dotnet-sdk-5.0.100-linux-x64.tar.gz -C $HOME/dotnet   (unzip the file to dotnet folder)

ls $HOME/dotnet   (make sure unzip result exist)

```

- Set PATH for dotnet sdk on vps
```
nano ~/.bashrc  (or your profile file)
```

- Add this 2 lines at the end of  your bashrc profile

```
export DOTNET_ROOT=$HOME/dotnet
export PATH=$PATH:$HOME/dotnet
```

- save your bash profile buy press ctrl+x and yes in your keyboard


- activate your bash profile
```
source ~/.bashrc
```

- Create folder ukc on vps server

```
mkdir $HOME/ukc 
```


- Open other terminal in your Laptop/PC and build UbudKusCoin for linux 

```
cd UbudKusCoin/UbudKusCoin
dotnet publish -c Release -r linux-x64 -o ./publish-linux
```

- Zip all files of build result
build result take location in publish-linux folder
```
cd publish-linux
zip -r archive.zip .  (with dot at the end)
```


- Copy file to vps (virtual private server) 
```
scp -i ~/SSH/ssh.pem archive.zip root@xxx.xxx.xxx.xxx:~/ukc
```

- Unzip archive.zip on your vps server
- connect to your vps again, see above command to connect to your vps

```
cd ~/ukc 
unzip archive.zip
```

- run ukc core
```
~/ukc/UbudKusCoin
```
ubudkus coin core will run, but when terminal closed, it will stoped, follow next step
how run ubudkuscoin as service

- Open/allow Port 5002 in your firewall setting, so it can access from client side. in aws lightsaill, there is network menu, edit it.


## Run as service on linux vps

- copy/upload file UbudKusCoin.service to vps folder 
/etc/systemd/system

so location will be /etc/systemd/system/UbudKusCoin.service


- Start the service

```
sudo systemctl daemon-reload
sudo systemctl start UbudKusCoin
sudo systemctl enable UbudKusCoin


```
- Stop the service

```
sudo systemctl stop UbudKusCoin

```


for more detail see end of this file








Please read this articles to know more detail how to run app as service on linux

References:

- https://docs.microsoft.com/en-us/dotnet/architecture/grpc-for-wcf-developers/self-hosted


- https://stackoverflow.com/questions/63827667/bind-grpc-services-to-specific-port-in-aspnetcore

- https://swimburger.net/blog/dotnet/how-to-run-a-dotnet-core-console-app-as-a-service-using-systemd-on-linux

- https://docs.microsoft.com/en-us/aspnet/core/grpc/browser?view=aspnetcore-6.0#configure-grpc-web-in-aspnet-core

- https://docs.microsoft.com/en-us/aspnet/core/grpc/browser?view=aspnetcore-6.0

- .Net Core with TLS
https://andrewlock.net/creating-and-trusting-a-self-signed-certificate-on-linux-for-use-in-kestrel-and-asp-net-core/



