"""MCP: pages, shapes and text — the foundation every other workflow builds on."""

from __future__ import annotations

import pytest

from pytest_aitest import Agent, Provider

from conftest import (
    DEFAULT_RETRIES,
    DEFAULT_TIMEOUT_MS,
    assert_used_tool,
    unique_path,
)

pytestmark = [pytest.mark.aitest, pytest.mark.mcp]


def _agent(name: str, server, skill, max_turns: int = 25) -> Agent:
    return Agent(
        name=name,
        provider=Provider(model="azure/gpt-4.1", rpm=10, tpm=10000),
        mcp_servers=[server],
        skill=skill,
        max_turns=max_turns,
        retries=DEFAULT_RETRIES,
    )


@pytest.mark.asyncio
async def test_mcp_creates_a_drawing_with_named_pages(aitest_run, visio_mcp_server, visio_mcp_skill):
    agent = _agent("mcp-pages", visio_mcp_server, visio_mcp_skill)

    prompt = f"""
Create a new Visio drawing at {unique_path('regions')}

Give it two pages, one named "Overview" and one named "Detail".

On the Overview page, add a shape labelled "Europe" and a shape labelled "Asia".

Save the drawing when done.
"""
    result = await aitest_run(agent, prompt, timeout_ms=DEFAULT_TIMEOUT_MS)

    assert result.success
    assert_used_tool(result, "page")
    assert_used_tool(result, "shape")


@pytest.mark.asyncio
async def test_mcp_reads_back_what_it_created(aitest_run, visio_mcp_server, visio_mcp_skill):
    """A drawing that was written is not necessarily a drawing that exists."""
    agent = _agent("mcp-readback", visio_mcp_server, visio_mcp_skill)

    prompt = f"""
Create a new Visio drawing at {unique_path('readback')}

Add three shapes to page 1 with the labels "Alpha", "Beta" and "Gamma".

Then list the shapes on page 1 and tell me exactly which labels you find.
"""
    result = await aitest_run(agent, prompt, timeout_ms=DEFAULT_TIMEOUT_MS)

    assert result.success
    for label in ("Alpha", "Beta", "Gamma"):
        assert label in result.output, f"The agent did not report the shape it created: {label}"


@pytest.mark.asyncio
async def test_mcp_sets_page_size_before_placing_shapes(aitest_run, visio_mcp_server, visio_mcp_skill):
    """Page size lives in PageSheet cells, not in a page property."""
    agent = _agent("mcp-pagesize", visio_mcp_server, visio_mcp_skill)

    prompt = f"""
Create a new Visio drawing at {unique_path('landscape')}

Make page 1 exactly 17 inches wide and 11 inches tall, then add one shape
labelled "Wide canvas" near the centre of the page.

Save the drawing.
"""
    result = await aitest_run(agent, prompt, timeout_ms=DEFAULT_TIMEOUT_MS)

    assert result.success
    # Page size is a PageSheet cell — an agent reaching for a page property would fail.
    assert_used_tool(result, "cell")
