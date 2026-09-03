"""CLI: pages, shapes and text through visiocli."""

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


def _agent(name: str, server, skill, max_turns: int = 28) -> Agent:
    return Agent(
        name=name,
        provider=Provider(model="azure/gpt-4.1", rpm=10, tpm=10000),
        cli_servers=[server],
        skill=skill,
        max_turns=max_turns,
        retries=DEFAULT_RETRIES,
    )


@pytest.mark.asyncio
async def test_cli_creates_a_drawing_with_named_pages(aitest_run, visio_cli_server, visio_cli_skill):
    agent = _agent("cli-pages", visio_cli_server, visio_cli_skill)

    prompt = f"""
Using the visiocli tool, create a new Visio drawing at {unique_path('cli-regions')}

Give it two pages, one named "Overview" and one named "Detail".
Add a shape labelled "Europe" to the Overview page.

Close the session with save when done.
"""
    result = await aitest_run(agent, prompt, timeout_ms=DEFAULT_TIMEOUT_MS)

    assert result.success
    assert_cli_exit_codes(result)
    assert_cli_args_contain(result, "page")


@pytest.mark.asyncio
async def test_cli_closes_its_session(aitest_run, visio_cli_server, visio_cli_skill):
    """A session left open holds the file, so the next run cannot touch it."""
    agent = _agent("cli-session", visio_cli_server, visio_cli_skill, max_turns=20)

    prompt = f"""
Using the visiocli tool, create a new Visio drawing at {unique_path('cli-session')},
add one shape labelled "Only shape" to page 1, then close the session saving changes.

Finally, list the open sessions and tell me how many remain.
"""
    result = await aitest_run(agent, prompt, timeout_ms=DEFAULT_TIMEOUT_MS)

    assert result.success
    assert_cli_exit_codes(result)
    assert_cli_args_contain(result, "close")


@pytest.mark.asyncio
async def test_cli_reads_back_what_it_created(aitest_run, visio_cli_server, visio_cli_skill):
    agent = _agent("cli-readback", visio_cli_server, visio_cli_skill)

    prompt = f"""
Using the visiocli tool, create a new Visio drawing at {unique_path('cli-readback')},
add shapes labelled "Alpha" and "Beta" to page 1, then list the shapes on page 1
and tell me exactly which labels you find.

Close the session when done.
"""
    result = await aitest_run(agent, prompt, timeout_ms=DEFAULT_TIMEOUT_MS)

    assert result.success
    assert_cli_exit_codes(result)
    for label in ("Alpha", "Beta"):
        assert label in result.output, f"The agent did not report the shape it created: {label}"
