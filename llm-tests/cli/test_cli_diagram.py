"""CLI: diagram workflows — stencil masters, connectors and design guidance."""

from __future__ import annotations

import pytest

from pytest_aitest import Agent, Provider

from conftest import (
    DEFAULT_RETRIES,
    DEFAULT_TIMEOUT_MS,
    assert_cli_args_contain,
    assert_cli_exit_codes,
    unique_path,
)

pytestmark = [pytest.mark.aitest, pytest.mark.cli]


def _agent(name: str, server, skill, max_turns: int = 32) -> Agent:
    return Agent(
        name=name,
        provider=Provider(model="azure/gpt-4.1", rpm=10, tpm=10000),
        cli_servers=[server],
        skill=skill,
        max_turns=max_turns,
        retries=DEFAULT_RETRIES,
    )


@pytest.mark.asyncio
async def test_cli_builds_a_connected_flowchart(aitest_run, visio_cli_server, visio_cli_skill):
    """Shapes placed but never joined is the way generated output is most often wrong."""
    agent = _agent("cli-flowchart", visio_cli_server, visio_cli_skill)

    prompt = f"""
Using the visiocli tool, create a new Visio drawing at {unique_path('cli-flowchart')}

Build a flowchart on page 1 from the Basic Flowchart stencil:
- Start/End labelled "Request received"
- Process labelled "Validate"
- Decision labelled "Approved?"

Connect them in that order, then list the connectors on page 1 and tell me how many exist.

Close the session with save.
"""
    result = await aitest_run(agent, prompt, timeout_ms=DEFAULT_TIMEOUT_MS)

    assert result.success
    assert_cli_exit_codes(result)

    # Dropping masters rather than drawing rectangles.
    assert_cli_args_contain(result, "stencil")
    assert_cli_args_contain(result, "connect-shapes")


@pytest.mark.asyncio
async def test_cli_consults_the_design_catalog_first(aitest_run, visio_cli_server, visio_cli_skill):
    agent = _agent("cli-guidance", visio_cli_server, visio_cli_skill, max_turns=20)

    prompt = """
Using the visiocli tool, find out which diagram archetype applies to a network diagram,
and tell me exactly which stencil file and master names it recommends.

Do not create any file.
"""
    result = await aitest_run(agent, prompt, timeout_ms=DEFAULT_TIMEOUT_MS)

    assert result.success
    assert_cli_exit_codes(result)
    assert_cli_args_contain(result, "design")

    # The catalog names it; an agent guessing would not produce the stencil file name.
    assert "PERIPH_M" in result.output.upper(), "The agent did not report the network stencil."


@pytest.mark.asyncio
async def test_cli_discovers_commands_before_using_them(aitest_run, visio_cli_server, visio_cli_skill):
    """The skill's first rule: run --help before guessing at a command."""
    agent = _agent("cli-discovery", visio_cli_server, visio_cli_skill, max_turns=16)

    prompt = """
Using the visiocli tool, find out what it can do with layers and describe the
available actions. Do not create any file.
"""
    result = await aitest_run(agent, prompt, timeout_ms=DEFAULT_TIMEOUT_MS)

    assert result.success
    assert_cli_args_contain(result, "--help")
