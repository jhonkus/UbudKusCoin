# UbudKusCoin

NOTE: PLEASE DON'T USE DEVELOP BRANCH.
I am still working on it, its not working.
It will finish it on 05 January 2022.


Simple Cryptocurrencies with Proof Of Stake  Consensus Algorithm.

Developed with C# and .Net Core 5.0

This Solution have 3 projects

- UbudKusCoin  is Blockchain Core
- ConsoleWallet is desktop wallet
- BlockExporer is desktop explorer


## Requirement
Net SDK 5.0 https://dotnet.microsoft.com/download/dotnet/5.0

## How Install Net SDK 5.0
- download https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/sdk-5.0.100-linux-x64-binaries
- cd ~/Downloads  (assume the sdk downloaded in Downloads folter)
- mkdir -p $HOME/dotnet && tar zxf dotnet-sdk-5.0.100-linux-x64.tar.gz -C $HOME/dotnet
- export DOTNET_ROOT=$HOME/dotnet
- export PATH=$PATH:$HOME/dotnet

## IDE
- Visual Studio Comunity Edition https://visualstudio.microsoft.com/downloads
- For linux user VSCode, follow instruction in this website  https://code.visualstudio.com/docs/languages/dotnet 

## Instalation

First install .Net Core SDK 5.0


```
> git clone https://github.com/jhonkus/UbudKusCoin.git

> cd UbudKusCoin

> dotnet restore

```

To run Blockchain core, after above command

```
> cd UbudKuscoin
> dotnet run

```

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



My Articles:

- Part 1

https://putukusuma.medium.com/developing-simple-crypto-application-using-c-48258c2d4e45

- part 2

https://putukusuma.medium.com/creating-simple-cryptocurrency-using-c-and-net-core-part-2-menu-and-database-4ae842098f55

- Part3

https://putukusuma.medium.com/creating-simple-cryptocurrency-using-c-and-net-core-part-3-wallet-8bbfe0544770

- Part4

https://putukusuma.medium.com/creating-simple-cryptocurrency-using-c-and-net-part-4-block-header-c8ad97fd237b


My Videos:

- https://youtu.be/TYM55x7I8us

- https://youtu.be/gpYKUWGBxf4


