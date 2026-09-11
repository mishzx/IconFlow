# Privacy

IconFlow is designed as an offline, local-first Windows utility.

## Data processed locally

To replace and restore icons, IconFlow may read the paths and icon metadata of folders or shortcuts explicitly selected by the user. Imported images, generated ICO files, backups, preferences and change history are stored locally under `%LocalAppData%\IconFlow`, or under a library location selected by the user.

## Data not collected

The application contains no analytics or telemetry service and does not upload:

- file or folder names and paths;
- desktop contents;
- installed application lists;
- imported icons or source images;
- operation history;
- device identifiers or account information.

IconFlow does not require an account and does not continuously access the network.

## Windows integration

The compatibility context menu writes only current-user registry entries. The optional Windows 11 first-level menu installs a sparse identity package and its public development certificate after explicit UAC approval. Both integrations can be removed from Settings or with the included uninstall script.

## User control

The icon library can be moved from Settings. Uninstalling the application does not silently delete user-created icons or backups; users should choose whether to preserve applied icons or restore defaults first.

## Security reports

Please follow [SECURITY.md](SECURITY.md) instead of disclosing a vulnerability in a public issue.
