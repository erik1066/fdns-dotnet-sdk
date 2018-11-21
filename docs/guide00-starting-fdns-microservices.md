# How to run the Foundation Services for local development

The code from our quick-start guides won't work if the Foundation Services aren't running. There are a few ways you can start the Foundation Services:

## Running one Foundation Service locally

If you're building an app and only need to use one Foundation Service, such as the [FDNS Object microservice](https://github.com/CDCGov/fdns-ms-object), the simplest option is to pull that service down and run it locally:

```bash
git clone https://github.com/CDCgov/fdns-ms-object.git
cd fdns-ms-object
make docker-build
make docker-start
```

> The `make docker-build` command will take about a minute to run.

You should now be able to visit `http://localhost:8083` in your browser to verify the `fdns-ms-object` service is running.

## Running all Foundation Services locally

If you need to run multiple Foundation Services, clone the [FDNS Gateway microservice](https://github.com/CDCGov/fdns-ms-gateway) and start it locally. Doing so starts _all_ of the open source Foundation Services via a `docker-compose` script.

> You will need to devote >6 GB of memory to Docker for Windows/Mac to reliably start all of the services

The steps are quite similar to those shown above:

```bash
git clone https://github.com/CDCgov/fdns-ms-gateway.git
cd fdns-ms-gateway
make docker-build
make docker-start
```

## Running a subset of Foundation Services locally

Running the [FDNS Gateway microservice](https://github.com/CDCGov/fdns-ms-gateway) as described inthe previous section requires a lot of memory. To reduce memory usage, you can comment out any of the Foundation Services you want to skip spinning up as part of Gateway's `docker-compose.yml` instructions.

> Comments in `yml` files start with the `#` character.

Then, simply restart the `fdns-ms-gateway` container (you don't need to rebuild it):

```bash
make docker-restart
```


