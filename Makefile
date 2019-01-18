sonar:
	docker pull sonarqube
	docker run -d --name sonarqube -p 9000:9000 -p 9092:9092 sonarqube || true
	printf 'Wait for sonarqube\n'
	until `curl --output /dev/null --silent --head --fail --connect-timeout 80 http://localhost:9000/api/server/version`; do printf '.'; sleep 1; done
	sleep 7
	dotnet tool install --global dotnet-sonarscanner || true
	dotnet sonarscanner begin /k:"fdns-dotnet-sdk" || true
	dotnet test --collect:"Code Coverage"
	dotnet sonarscanner end || true

sonar-stop:
	docker kill sonarqube || true
	docker rm sonarqube || true
