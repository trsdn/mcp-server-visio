"""Fixtures and helpers for VisioMcp LLM integration tests."""

from __future__ import annotations

import json
import os
import re
import shlex
import tempfile
import uuid
from pathlib import Path
from typing import Any

import pytest

from pytest_aitest import CLIServer, MCPServer, Skill, Wait

TESTS_DIR = Path(__file__).resolve().parent
REPO_ROOT = TESTS_DIR.parent
TEST_RESULTS_DIR = TESTS_DIR / "TestResults"
TEST_RESULTS_DIR.mkdir(parents=True, exist_ok=True)

# Skill references are copied at build time by MSBuild targets in the CLI and MCP csproj files.
# Run 'dotnet build -c Release' to update them.

# Pydantic AI uses AZURE_API_BASE for Azure OpenAI endpoint discovery.
if os.environ.get("AZURE_OPENAI_ENDPOINT") and not os.environ.get("AZURE_API_BASE"):
    os.environ["AZURE_API_BASE"] = os.environ["AZURE_OPENAI_ENDPOINT"]

DEFAULT_MODEL = "gpt-4.1"
DEFAULT_RPM = 10
DEFAULT_TPM = 10000
DEFAULT_MAX_TURNS = 20
DEFAULT_RETRIES = 3  # Visio COM operations need more retries than the default of 1
DEFAULT_TIMEOUT_MS = 600000  # 10 min — Azure GlobalStandard can be slow under load

# The framework the repository actually targets. This previously read net10.0-windows, so the
# built executable was never found and every run silently fell back to `dotnet run`.
TARGET_FRAMEWORK = "net9.0-windows"


def pytest_configure(config: pytest.Config) -> None:
    azure_base = os.environ.get("AZURE_API_BASE") or os.environ.get("AZURE_OPENAI_ENDPOINT")
    if azure_base:
        config.option.llm_model = "azure/gpt-4.1"


def unique_path(prefix: str, suffix: str = ".vsdx") -> str:
    temp_dir = Path(os.environ.get("TEMP", tempfile.gettempdir()))
    path = temp_dir / f"{prefix}-{uuid.uuid4()}{suffix}"
    return path.as_posix()


def unique_results_path(prefix: str, suffix: str = ".vsdx") -> str:
    path = TEST_RESULTS_DIR / f"{prefix}-{uuid.uuid4()}{suffix}"
    return path.as_posix()


def assert_regex(text: str, pattern: str) -> None:
    if not re.search(pattern, text, re.IGNORECASE | re.MULTILINE):
        raise AssertionError(f"Pattern not found: {pattern}\nText:\n{text}")


def _parse_cli_results(result: Any) -> list[dict[str, Any]]:
    calls = result.tool_calls_for("visio_execute")
    outputs: list[dict[str, Any]] = []
    for call in calls:
        if call.result:
            try:
                outputs.append(json.loads(call.result))
            except json.JSONDecodeError:
                outputs.append({"exit_code": -1, "stdout": call.result, "stderr": ""})
    return outputs


def assert_cli_exit_codes(result: Any, *, strict: bool = False) -> None:
    """Assert CLI executions succeeded.

    By default the last call must have succeeded. LLMs retry after an error, and punishing
    recovery discourages it. With strict=True every call must succeed.
    """
    outputs = _parse_cli_results(result)
    if not outputs:
        raise AssertionError("No CLI executions recorded")
    if strict:
        for output in outputs:
            if output.get("exit_code") != 0:
                raise AssertionError(f"CLI exit code not zero: {output}")
    else:
        last = outputs[-1]
        if last.get("exit_code") != 0:
            raise AssertionError(
                f"Final CLI call failed (exit_code={last.get('exit_code')}): "
                f"{last.get('stdout', '')[:200]}"
            )
        failed = sum(1 for o in outputs if o.get("exit_code") != 0)
        if failed > len(outputs) * 0.8:
            raise AssertionError(f"Too many CLI failures: {failed}/{len(outputs)} calls failed")


def assert_cli_args_contain(result: Any, token: str) -> None:
    calls = result.tool_calls_for("visio_execute")
    for call in calls:
        args = call.arguments.get("args", "")
        if token in args:
            return
    raise AssertionError(f"Expected CLI args to include '{token}', but none did.")


def assert_used_tool(result: Any, tool_name: str) -> None:
    """Assert the agent actually reached for a given MCP tool.

    A prompt can be satisfied by the wrong means — drawing a rectangle where a stencil master was
    required, for instance — and the drawing still looks plausible. Asserting the tool asserts the
    method, not just the outcome.
    """
    if not result.tool_calls_for(tool_name):
        raise AssertionError(f"Expected the agent to call '{tool_name}', but it never did.")


def _resolve_mcp_command() -> list[str]:
    env_command = os.environ.get("MCP_SERVER_COMMAND")
    if env_command:
        return shlex.split(env_command)

    exe_path = REPO_ROOT / f"src/VisioMcp.McpServer/bin/Release/{TARGET_FRAMEWORK}/VisioMcp.McpServer.exe"
    if exe_path.exists():
        return [str(exe_path)]

    project_path = REPO_ROOT / "src/VisioMcp.McpServer/VisioMcp.McpServer.csproj"
    return ["dotnet", "run", "--project", str(project_path), "-c", "Release", "--no-build"]


def _resolve_cli_command() -> str:
    env_command = os.environ.get("CLI_COMMAND")
    if env_command:
        return env_command

    exe_path = REPO_ROOT / f"src/VisioMcp.CLI/bin/Release/{TARGET_FRAMEWORK}/visiocli.exe"
    if exe_path.exists():
        return str(exe_path)

    return "visiocli"


@pytest.fixture(scope="session")
def visio_mcp_server() -> MCPServer:
    return MCPServer(
        command=_resolve_mcp_command(),
        wait=Wait.ready(timeout_ms=30000),
    )


@pytest.fixture(scope="session")
def visio_cli_server() -> CLIServer:
    command = _resolve_cli_command()
    temp_dir = Path(os.environ.get("TEMP", tempfile.gettempdir()))
    return CLIServer(
        name="visio-cli",
        command=command,
        tool_prefix="visio",
        shell="none",
        cwd=str(temp_dir),
        discover_help=False,  # Skill Rule 0 requires the LLM to run --help first
        description="Visio CLI automation. Run 'visiocli --help' to discover available commands before use.",
        timeout=120.0,  # Visio COM operations, especially session close, can take >30s
    )


@pytest.fixture(scope="session")
def visio_mcp_skill() -> Skill:
    return Skill.from_path(REPO_ROOT / "skills/visio-mcp")


@pytest.fixture(scope="session")
def visio_cli_skill() -> Skill:
    return Skill.from_path(REPO_ROOT / "skills/visio-cli")


@pytest.fixture(scope="session")
def results_dir() -> Path:
    return TEST_RESULTS_DIR
