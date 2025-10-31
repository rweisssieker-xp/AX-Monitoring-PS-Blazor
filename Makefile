.PHONY: help install install-dev test lint format clean run docker-build docker-run deploy

help: ## Show this help message
	@echo "Available commands:"
	@grep -E '^[a-zA-Z_-]+:.*?## .*$$' $(MAKEFILE_LIST) | sort | awk 'BEGIN {FS = ":.*?## "}; {printf "\033[36m%-20s\033[0m %s\n", $$1, $$2}'

install: ## Install production dependencies
	pip install -r requirements.txt

install-dev: ## Install development dependencies
	pip install -r requirements.txt
	pip install pytest pytest-cov pytest-mock black isort flake8 mypy pre-commit

test: ## Run tests
	pytest tests/ -v --cov=app --cov-report=html --cov-report=term

test-unit: ## Run unit tests only
	pytest tests/ -v -m "unit" --cov=app

test-integration: ## Run integration tests only
	pytest tests/ -v -m "integration"

lint: ## Run linting
	flake8 app tests
	mypy app --ignore-missing-imports

format: ## Format code
	black app tests
	isort app tests

format-check: ## Check code formatting
	black --check app tests
	isort --check-only app tests

clean: ## Clean up temporary files
	find . -type f -name "*.pyc" -delete
	find . -type d -name "__pycache__" -delete
	find . -type d -name "*.egg-info" -exec rm -rf {} +
	rm -rf build/
	rm -rf dist/
	rm -rf .coverage
	rm -rf htmlcov/
	rm -rf .pytest_cache/
	rm -rf .mypy_cache/

run: ## Run the application locally
	streamlit run app/app.py

run-dev: ## Run the application in development mode
	streamlit run app/app.py --server.runOnSave true


setup-pre-commit: ## Setup pre-commit hooks
	pre-commit install

security-check: ## Run security checks
	safety check
	bandit -r app/

build: ## Build the package
	python -m build

install-package: ## Install the package in development mode
	pip install -e .

docs: ## Generate documentation
	sphinx-build -b html docs/ docs/_build/html

docs-serve: ## Serve documentation locally
	cd docs/_build/html && python -m http.server 8000

deploy-staging: ## Deploy to staging environment
	@echo "Deploying to staging..."
	# Add your staging deployment commands here

deploy-prod: ## Deploy to production environment
	@echo "Deploying to production..."
	# Add your production deployment commands here

ci: lint test security-check ## Run CI pipeline locally
