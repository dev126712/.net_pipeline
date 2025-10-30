
### Build
```bash
sudo docker build . -t test2:latest
```

### Run
```bash
sudo docker run -d -p 8080:8080 -e ASPNETCORE_ENVIRONMENT="Development" test2:latest
```
