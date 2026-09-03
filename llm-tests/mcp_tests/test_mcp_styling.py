"""MCP: formatting through ShapeSheet cells and named styles."""

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


def _agent(name: str, server, skill, max_turns: int = 28) -> Agent:
    return Agent(
        name=name,
        provider=Provider(model="azure/gpt-4.1", rpm=10, tpm=10000),
        mcp_servers=[server],
        skill=skill,
        max_turns=max_turns,
        retries=DEFAULT_RETRIES,
    )


@pytest.mark.asyncio
async def test_mcp_fills_and_outlines_shapes(aitest_run, visio_mcp_server, visio_mcp_skill):
    agent = _agent("mcp-fill", visio_mcp_server, visio_mcp_skill)

    prompt = f"""
Create a new Visio drawing at {unique_path('styling')}

Add three shapes to page 1 labelled "Green", "Amber" and "Red", and fill each one
with a colour matching its label. Give every shape a 2pt outline.

Then read back the fill colour of the shape labelled "Red" and tell me the value.
"""
    result = await aitest_run(agent, prompt, timeout_ms=DEFAULT_TIMEOUT_MS)

    assert result.success
    assert_used_tool(result, "shape")


@pytest.mark.asyncio
async def test_mcp_uses_a_named_style_for_repeated_formatting(aitest_run, visio_mcp_server, visio_mcp_skill):
    """Changing a style restyles every shape using it — the reason to use one."""
    agent = _agent("mcp-style", visio_mcp_server, visio_mcp_skill)

    prompt = f"""
Create a new Visio drawing at {unique_path('styles')}

Add four shapes to page 1 labelled "API", "Worker", "Cache" and "Queue".

Create a named style called "Deprecated" that gives shapes a dashed outline,
and apply it to the "Cache" and "Queue" shapes only.

Save the drawing.
"""
    result = await aitest_run(agent, prompt, timeout_ms=DEFAULT_TIMEOUT_MS)

    assert result.success
    assert_used_tool(result, "style")


@pytest.mark.asyncio
async def test_mcp_puts_annotations_on_their_own_layer(aitest_run, visio_mcp_server, visio_mcp_skill):
    """A layer is what makes commentary removable without rebuilding the drawing."""
    agent = _agent("mcp-layers", visio_mcp_server, visio_mcp_skill)

    prompt = f"""
Create a new Visio drawing at {unique_path('annotated')}

Add two shapes to page 1 labelled "Ingest" and "Store", and connect them.

Add a note shape labelled "Bottleneck here" and put it on a layer called "Annotations",
so the note can be hidden without touching the rest of the drawing.

Then hide that layer and save the drawing.
"""
    result = await aitest_run(agent, prompt, timeout_ms=DEFAULT_TIMEOUT_MS)

    assert result.success
    assert_used_tool(result, "layer")


@pytest.mark.asyncio
async def test_mcp_stores_metadata_as_shape_data_not_labels(aitest_run, visio_mcp_server, visio_mcp_skill):
    """Data the drawing must hold but need not show belongs in shape data."""
    agent = _agent("mcp-shapedata", visio_mcp_server, visio_mcp_skill)

    prompt = f"""
Create a new Visio drawing at {unique_path('network')}

Add two shapes to page 1 labelled "web-01" and "web-02".

Record each one's IP address as shape data rather than in its label:
web-01 is 10.0.1.15 and web-02 is 10.0.1.16.

Then read back the shape data for web-01 and tell me what you find.
"""
    result = await aitest_run(agent, prompt, timeout_ms=DEFAULT_TIMEOUT_MS)

    assert result.success
    assert "10.0.1.15" in result.output, "The agent did not report the shape data it stored."
