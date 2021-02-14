# UbudKusCoin
Simple Cryptocurrencies with Proof Of Stake  Consensus Algorithm.

Developed with C# and .Net 5.0

This Solution have 3 projects

- UbudKusCoin  is Blockchain Core
- ConsoleWallet is desktop wallet
- BlockExporer is desktop explorer


## Requirement
Net SDK 5.0 https://dotnet.microsoft.com/download/dotnet/5.0

## IDE
- Visual Studio Comunity Edition https://visualstudio.microsoft.com/downloads
- For linux user download Monodevelop https://www.monodevelop.com

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

Open Project with Visual Studio Comunity Edition 2019 or Monodevelop.

## Publish

- Net Runtime

```
dotnet publish -c Release -o ./publish-net
```

- Linux

```
dotnet publish -c Release -r linux-x64 -o ./publish-linux
```

## Copy file to server
```
scp Archive.zip root@xx.15.xx1.xx:~/path
```

## Install UNZIP

```
sudo apt-get install unzip

unzip file.zip -d destination_folder
unzip file.zip
```

Articles:

Part 1
https://putukusuma.medium.com/developing-simple-crypto-application-using-c-48258c2d4e45

part 2
https://putukusuma.medium.com/creating-simple-cryptocurrency-using-c-and-net-core-part-2-menu-and-database-4ae842098f55

Part3
https://putukusuma.medium.com/creating-simple-cryptocurrency-using-c-and-net-core-part-3-wallet-8bbfe0544770


Videos
https://youtu.be/TYM55x7I8us

https://youtu.be/gpYKUWGBxf4





Self-hosted gRPC applications

https://docs.microsoft.com/en-us/dotnet/architecture/grpc-for-wcf-developers/self-hosted



Reference
https://stackoverflow.com/questions/63827667/bind-grpc-services-to-specific-port-in-aspnetcore
