# ldbdump

Source: https://github.com/golang/leveldb/tree/master/cmd/ldbdump

## About `ldbdump.exe`

Ldbdump.exe is a pre-built binary used to dump the contents of a LevelDB key-value store. Why dump the whole content? Because it seems to be impossible to find a .NET library for reading LevelDB stores. If you happen to know any please let me know.

The Teams desktop client, Chrome and Chromium-based Edge all use LevelDB files to store local and session storage.

_Note: Local and session storage will contain sensitive information like security tokens. Be careful as to where you put the output of ldbdump.exe._

## Build `ldbdump` yourself

### Windows
You can built the ldbdump tool yourself if you don't want to use the pre-built binary: 

* install [Go for Windows](https://golang.org/doc/install) (I used v1.15.8)
* clone the [leveldb GitHub repository](https://github.com/golang/leveldb)
* open a console window and change to the `leveldb\cmd\ldbdump` folder of the cloned repo
* run `go build -o ldbdump.exe main.go`

You should now have a working `ldbdump.exe`. Test it with any ldb file from your Google Chrome local storage which is located at `C:\Users\<user>\AppData\Local\Google\Chrome\User Data\Default\Local Storage\leveldb`.

Here's a sample command which should output all of Chrome's local storage as text:

```
ldbdump "C:\Users\<user>\AppData\Local\Google\Chrome\User Data\Default\Local Storage\leveldb\006564.ldb"
```
_Note: The file names of the ldb files change constantly._

### Linux

Tested on Fedora 33:
* `sudo dnf install go`
* create a folder `~/src/github.com/golang` - the build later will search for modules there
* open a console and go to `~/src/github.com/golang`
* `git clone https://github.com/golang/leveldb.git`
* `git clone https://github.com/golang/snappy.git`
* go to `~/src/github.com/golang/leveldb/cmd/ldbdump`
* `go build -o ldbdump main.go`

Now you have a working `ldbdump`.