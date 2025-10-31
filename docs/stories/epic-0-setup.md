# Epic 0: Project Setup & Quality Assurance

**Status:** Not Started  
**Priority:** Critical  
**Estimated Effort:** 2-3 days  
**Dependencies:** None

---

## Epic Goal

Establish the foundational project structure, development environment, tooling, and quality gates to enable efficient and maintainable development of the AX Performance Monitor.

---

## User Story

**As a** Developer  
**I want** a fully configured project scaffold with quality tools  
**So that** I can start building features immediately with consistent code quality and automated checks

---

## Acceptance Criteria

### AC1: Project Structure Created
- [ ] Directory structure matches `docs/architecture/source-tree.md`
- [ ] All core directories exist: `app/`, `tests/`, `docs/`, `app/sql/`
- [ ] Empty `__init__.py` files in all Python packages
- [ ] `.gitignore` configured for Python, `.env`, logs, caches

### AC2: Streamlit Multi-Page App Functional
- [ ] `app/main.py` entry point created
- [ ] Streamlit runs successfully: `streamlit run app/main.py`
- [ ] Basic landing page displays with project title
- [ ] Multi-page structure working (placeholder pages)
- [ ] No errors in console on startup

### AC3: Dependency Management Configured
- [ ] `requirements.txt` with pinned versions:
  - streamlit==1.31.0
  - pandas==2.0.3
  - plotly==5.18.0
  - pyodbc==5.0.1 (or pymssql==2.2.8)
  - APScheduler==3.10.4
  - python-dotenv==1.0.0
  - cachetools==5.3.2
  - structlog==23.2.0
- [ ] `dev-requirements.txt` with dev tools:
  - pytest==7.4.3
  - pytest-cov==4.1.0
  - pytest-mock==3.12.0
  - ruff==0.1.6
  - black==23.11.0
  - mypy==1.7.1
  - pre-commit==3.5.0
- [ ] `pip install -r requirements.txt` succeeds without errors
- [ ] Virtual environment documented in README

### AC4: Code Quality Tools Configured
- [ ] `pyproject.toml` configured with ruff, black, mypy settings
- [ ] `pytest.ini` configured with test paths and coverage settings
- [ ] `.pre-commit-config.yaml` configured with hooks:
  - ruff (linting + auto-fix)
  - black (formatting)
  - mypy (type checking)
  - trailing-whitespace
- [ ] Pre-commit hooks installed: `pre-commit install`
- [ ] `pre-commit run --all-files` passes on initial codebase

### AC5: Documentation Structure
- [ ] `README.md` updated with:
  - Project description
  - Setup instructions
  - Running instructions
  - Testing instructions
  - Contributing guidelines
- [ ] `docs/` directory structure created
- [ ] `docs/architecture/` contains tech-stack.md, coding-standards.md, source-tree.md
- [ ] `docs/prd.md` and `docs/architecture.md` exist

### AC6: Makefile for Common Tasks
- [ ] Makefile created with targets:
  - `make install` - Install dependencies
  - `make test` - Run pytest with coverage
  - `make lint` - Run ruff linting
  - `make format` - Run black formatting
  - `make typecheck` - Run mypy
  - `make run` - Start Streamlit app
  - `make clean` - Remove caches and temp files
- [ ] All Makefile targets execute successfully

---

## Technical Implementation Notes

### Project Initialization
```powershell
# Create project structure
mkdir -p app/{pages,services,db,scheduler,alerts,ui,sql/{schema,queries,migrations},utils}
mkdir -p tests/{unit,integration,e2e,fixtures}
mkdir -p docs/{architecture,sql}

# Create __init__.py files
New-Item app\__init__.py
New-Item app\pages\__init__.py
New-Item app\services\__init__.py
# ... (rest of packages)
```

### Streamlit Entry Point (`app/main.py`)
```python
import streamlit as st

st.set_page_config(
    page_title="AX Performance Monitor",
    page_icon="📊",
    layout="wide",
    initial_sidebar_state="expanded"
)

st.title("AX 2012 R3 Performance Monitor")
st.markdown("""
Welcome to the AX Performance Monitoring Dashboard.

Select a page from the sidebar to begin.
""")
```

### Example pyproject.toml Configuration
```toml
[tool.ruff]
line-length = 100
target-version = "py310"

[tool.black]
line-length = 100
target-version = ['py310']

[tool.mypy]
python_version = "3.10"
warn_return_any = true
warn_unused_configs = true
disallow_untyped_defs = true

[tool.pytest.ini_options]
testpaths = ["tests"]
python_files = "test_*.py"
addopts = "--cov=app --cov-report=html --cov-report=term"
```

---

## Definition of Done

- [ ] All acceptance criteria met
- [ ] Code passes all pre-commit hooks (`pre-commit run --all-files`)
- [ ] `make test` passes (even if tests are minimal placeholders)
- [ ] `make lint` passes with no errors
- [ ] `make typecheck` passes with no errors
- [ ] `streamlit run app/main.py` starts without errors
- [ ] README.md accurately reflects setup steps
- [ ] Documentation committed to Git
- [ ] Tagged in Git: `v0.1.0-setup`

---

## Tasks Breakdown

### Task 1: Create Directory Structure (30 min)
- Create all directories per source-tree.md
- Create `__init__.py` files
- Configure `.gitignore`

### Task 2: Configure Dependencies (45 min)
- Create `requirements.txt` with pinned versions
- Create `dev-requirements.txt`
- Create `pyproject.toml` with tool configs
- Test installation in clean venv

### Task 3: Setup Quality Tools (1 hour)
- Configure ruff, black, mypy in `pyproject.toml`
- Create `pytest.ini`
- Create `.pre-commit-config.yaml`
- Install and test pre-commit hooks

### Task 4: Create Makefile (30 min)
- Define all targets (install, test, lint, format, run, clean)
- Test each target

### Task 5: Streamlit Scaffold (45 min)
- Create `app/main.py`
- Create placeholder pages in `app/pages/`
- Test Streamlit startup
- Configure `streamlit.toml`

### Task 6: Documentation (1 hour)
- Update README.md with setup instructions
- Verify all docs exist and are current
- Add contributing guidelines

---

## Testing Strategy

### Unit Tests
- Placeholder test file: `tests/unit/test_placeholder.py`
```python
def test_placeholder():
    """Placeholder test to verify pytest works."""
    assert True
```

### Integration Tests
- None required for Epic 0

### Manual Testing
- [ ] Clone fresh repo, follow README setup steps
- [ ] Verify all Makefile targets work
- [ ] Verify Streamlit starts and displays landing page
- [ ] Verify pre-commit hooks trigger on commit

---

## Risks & Mitigations

| Risk | Impact | Mitigation |
|------|--------|------------|
| Python version mismatch | High | Document required Python 3.10+ in README |
| ODBC driver not available | Medium | Provide pymssql as fallback, document installation |
| Pre-commit hooks slow | Low | Configure hooks to run only on changed files |
| Virtual environment confusion | Medium | Document venv creation clearly in README |

---

## Dependencies

**Upstream:** None (first epic)

**Downstream:** All subsequent epics depend on this setup

---

## Related Documents

- `docs/architecture/tech-stack.md`
- `docs/architecture/coding-standards.md`
- `docs/architecture/source-tree.md`
- `README.md`

---

## Notes

- This epic establishes the development foundation
- Quality gates (linting, formatting, type checking) enforced from day 1
- Placeholder pages allow for parallel development in later epics
- Makefile provides consistent developer experience across team
