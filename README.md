# DeathEvents
Death Events is a plugin that alerts you to player deaths at the hands of another. Apart from this, it helps [Rust statistics](https://github.com/Katakurinna/server-manager) by injecting relevant information from a kill through the console.

Taking advantage of this, it uses the Rust Statistics API so that users can obtain information from the podium and its statistics on the server.

## Permissions

* `deathevents.stats` -- Allow user use `/stats` command
* `deathevents.podium` -- Allow user use `/podium` command

## Chat Commands

* `/stats`
* `/podium`

## Configuration

```json
{
    "Server Manager Server Id":1,
    "Server Manager Server Wipe Id":1,
    "Server Manager API URL":"http://rust.cerratolabs.me:8080/"
}
```

## Examples

![!podium command](https://i.gyazo.com/54ccb551a2437150e3722fb95b4dc391.png)

![!stats command](https://i.gyazo.com/9bcc03058d5614a52513b7e5fe24a1e8.png)

![Kill event message](https://i.gyazo.com/57e9e0f5deb417af44c7d16350cb3224.png)
