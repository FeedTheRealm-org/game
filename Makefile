COMPOSE := docker-compose.yml
EXEC_APP := ./entrypoint.dev.sh

help: # Show this help message
	@awk -F'#' '/^[^[:space:]].*:/ && !/^\.PHONY/ { \
		target = $$1; \
		comment = ($$2 ? $$2 : ""); \
		printf "  %-48s %s\n", target, comment \
	}' Makefile
.PHONY: help

down: # Stop and remove containers
	docker compose -f $(COMPOSE) --profile all down --remove-orphans -t 2
.PHONY: down

build: down # Build containers
	docker compose -f $(COMPOSE) --profile all build
.PHONY: build

up: down # Start containers [PRODUCTION LIKE]
	docker compose -f $(COMPOSE) --profile all up --force-recreate
.PHONY: up

up-build: down # Build and start containers [PRODUCTION LIKE]
	docker compose -f $(COMPOSE) --profile all up --build --force-recreate
.PHONY: up-build

up-db: down # Build and start mongo db
	docker compose -f $(COMPOSE) --profile db-only up --build
.PHONY: up-db

dev: # Starts DEBUG built server via entrypoint [DEVELOPMENT], use SERVER_ONLY=true for no DB
	@if [ "$$SERVER_ONLY" != "true" ]; then \
		docker compose -f $(COMPOSE) --profile db-only up --build --wait -d; \
	fi
	. ./.env && $(EXEC_APP) ./Build/Dev/server.x86_64 \
		-batchmode \
		-nographics \
		--world-id=$$WORLD_ID \
		--zone-id=$$ZONE_ID \
		--is-test-world=$$IS_TEST_WORLD \
		--port=$$PORT \
		--hport=$$HPORT
	@if [ "$$SERVER_ONLY" != "true" ]; then \
		docker compose -f $(COMPOSE) --profile db-only down; \
	fi
.PHONY: dev

clean: # Remove all containers, images and volumes
	docker compose -f $(COMPOSE) down --remove-orphans -v
.PHONY: clean

logs: # Tail logs of all containers
	docker compose -f $(COMPOSE) logs -f
.PHONY: logs

logs-%: # Tail logs of a specific service. Usage: make logs-service_name
	docker compose -f $(COMPOSE) logs -f $*
.PHONY: logs-%

db: # Open a mongosh shell in the mongo container
	docker compose -f $(COMPOSE) exec mongo mongosh -u admin -p admin
.PHONY: db
