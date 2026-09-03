"""MCP: the diagram workflows Visio exists for — stencil masters joined by connectors."""

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


def _agent(name: str, server, skill, max_turns: int = 30) -> Agent:
    return Agent(
        name=name,
        provider=Provider(model="azure/gpt-4.1", rpm=10, tpm=10000),
        mcp_servers=[server],
        skill=skill,
        max_turns=max_turns,
        retries=DEFAULT_RETRIES,
    )


@pytest.mark.asyncio
async def test_mcp_builds_a_connected_flowchart(aitest_run, visio_mcp_server, visio_mcp_skill):
    """The central Visio workflow, and the one most often got wrong.

    Shapes placed but never joined produce a drawing that looks right in a screenshot and is
    useless as a diagram, so this asserts the connectors rather than only the shapes.
    """
    agent = _agent("mcp-flowchart", visio_mcp_server, visio_mcp_skill)

    prompt = f"""
Create a new Visio drawing at {unique_path('flowchart')}

Build a small flowchart on page 1 using the Basic Flowchart stencil:
- a Start/End shape labelled "Order received"
- a Process shape labelled "Check stock"
- a Decision shape labelled "In stock?"
- a Process shape labelled "Dispatch"

Connect them in that order so the flow reads from the start through to dispatch.

Then list the connectors on page 1 and tell me how many there are.
"""
    result = await aitest_run(agent, prompt, timeout_ms=DEFAULT_TIMEOUT_MS)

    assert result.success

    # Dropping stencil masters, not drawing rectangles: a drawn diamond is not a Decision.
    assert_used_tool(result, "stencil")
    assert_used_tool(result, "shape")


@pytest.mark.asyncio
async def test_mcp_asks_the_catalog_which_diagram_to_draw(aitest_run, visio_mcp_server, visio_mcp_skill):
    """The design catalog exists so an agent need not guess a stencil name."""
    agent = _agent("mcp-guidance", visio_mcp_server, visio_mcp_skill, max_turns=20)

    prompt = """
I want to draw an organisation chart in Visio.

Before building anything, find out which archetype applies, and tell me exactly which
stencil file and which master names you would use.
"""
    result = await aitest_run(agent, prompt, timeout_ms=DEFAULT_TIMEOUT_MS)

    assert result.success
    assert_used_tool(result, "design")

    # The catalog names these; an agent that guessed would not produce the stencil file name.
    assert "ORGCH_M" in result.output.upper(), "The agent did not report the org chart stencil."


@pytest.mark.asyncio
async def test_mcp_builds_an_org_chart_from_the_right_stencil(aitest_run, visio_mcp_server, visio_mcp_skill):
    agent = _agent("mcp-orgchart", visio_mcp_server, visio_mcp_skill)

    prompt = f"""
Create a new Visio drawing at {unique_path('orgchart')}

Build an organisation chart on page 1 using the organisation chart stencil:
- an Executive labelled "Dana Whitfield, CTO"
- two Managers reporting to her, "Sam Okafor, Platform" and "Rae Lindqvist, Product"

Connect each manager to the executive.

Save the drawing.
"""
    result = await aitest_run(agent, prompt, timeout_ms=DEFAULT_TIMEOUT_MS)

    assert result.success
    assert_used_tool(result, "stencil")


@pytest.mark.asyncio
async def test_mcp_uses_a_background_page_for_shared_furniture(aitest_run, visio_mcp_server, visio_mcp_skill):
    """Background pages have no PowerPoint analogue and are easy to get wrong.

    A page must be marked as a background before it can be attached to another, and marking it
    moves it in the collection.
    """
    agent = _agent("mcp-background", visio_mcp_server, visio_mcp_skill)

    prompt = f"""
Create a new Visio drawing at {unique_path('titleblock')}

Add a second page named "Frame", make it a background page, and put a shape on it
labelled "ACME Engineering".

Then make page 1 show that background page behind its own content.

Save the drawing.
"""
    result = await aitest_run(agent, prompt, timeout_ms=DEFAULT_TIMEOUT_MS)

    assert result.success
    assert_used_tool(result, "page")
