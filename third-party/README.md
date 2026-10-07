# Third-party notices kept in the repository

`scripts/New-ThirdPartyNotices.ps1` builds THIRD-PARTY-NOTICES.txt for each release from the NuGet packages, the .NET and Windows Desktop runtime packs, and the WPF and Windows Forms repositories at the shipped runtime version. One part cannot be read from those sources and is kept here.

## Velopack's Rust components

Velopack's setup program, `Update.exe`, and the launcher are Rust programs built from many crates. `velopack-<version>-rust-notices.txt` holds their license texts for the Velopack version the app uses. The release fails if the file for that version is missing. After a Velopack update, generate it again with [cargo-about](https://github.com/EmbarkStudios/cargo-about):

```powershell
git clone --depth 1 --branch <version> https://github.com/velopack/velopack.git velopack-src
cd velopack-src
cargo about generate --manifest-path src/bins/Cargo.toml -c ../third-party/velopack-about.toml ../third-party/velopack-about.hbs -o ../third-party/velopack-<version>-rust-notices.txt
```

Edit the version in the first line of `velopack-about.hbs` first.
