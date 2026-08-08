# Third-Party Notices

UbudKusChain application code is released under the MIT License in
`LICENSE`. The following third-party components are used or referenced by the
repository and remain under their respective licenses.

## Protocol Schema

`UbudKusChain/Protos/cometbft_abci.proto` is an application-owned adaptation of
the CometBFT v0.38.17 ABCI schema. CometBFT is released under the Apache
License 2.0. The upstream schema is available at:

`https://github.com/cometbft/cometbft/blob/v0.38.17/proto/tendermint/abci/types.proto`

No CometBFT implementation source code is vendored in this repository.

## NuGet Dependencies

- `NBitcoin`: MIT License.
- `DotNetEnv`: MIT License.
- `Grpc.Net.Client`, `Grpc.AspNetCore`, and `Grpc.Tools`: Apache License 2.0.
- `Google.Protobuf`: BSD 3-Clause License.
- `LightningDB`: MIT License.
- `Newtonsoft.Json`: MIT License.
- .NET hosting and configuration packages: MIT License.

Dependency license and version metadata is authoritative in the restored NuGet
package files and should be regenerated during release compliance checks.
