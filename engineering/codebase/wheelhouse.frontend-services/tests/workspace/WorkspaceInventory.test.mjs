import assert from 'node:assert/strict';
import test from 'node:test';

import { loadWorkspaceModule, product, target } from './LoadWorkspaceModule.mjs';

const { buildWorkspaceInventory, WorkspaceProductBindings } = await loadWorkspaceModule('WorkspaceInventory');

test('joins only the reviewed registry slug and repository to its literal runner product', () => {
  const registry = product();
  const binding = target();
  const { products } = buildWorkspaceInventory([registry], [binding]);
  assert.equal(products.length, 1);
  assert.equal(products[0].binding, 'mapped');
  assert.equal(products[0].registry, registry);
  assert.equal(products[0].runnerProduct, 'foreverpin');
  assert.deepEqual(products[0].targets, [binding]);
  assert.equal(products[0].registry.status, 'Draft');
});

test('compares GitHub repository casing without changing runner or registry identifiers', () => {
  const { products } = buildWorkspaceInventory([product({ repo: ' Sulton-Max/10x-venture-Forever-Pin ' })], [target()]);
  assert.equal(products[0].binding, 'mapped');
  assert.equal(products[0].runnerProduct, 'foreverpin');
});

test('keeps unregistered targets available when the registry is empty', () => {
  const { products } = buildWorkspaceInventory([], [target()]);
  assert.deepEqual(products.map((entry) => [entry.key, entry.registry, entry.binding]),
    [['runner:foreverpin', null, 'unregistered']]);
});

test('does not infer a binding from equal-looking slugs', () => {
  const { products } = buildWorkspaceInventory([product({ slug: 'foreverpin' })], [target()]);
  assert.equal(products.length, 2);
  assert.equal(products[0].binding, 'unconfigured');
  assert.equal(products[0].runnerProduct, null);
  assert.deepEqual(products[0].targets, []);
  assert.equal(products[1].key, 'runner:foreverpin');
});

test('retains distinct literal runner product IDs including punctuation and case', () => {
  const { products } = buildWorkspaceInventory([], [target(),
    target({ id: 'hyphen', product: 'forever-pin' }), target({ id: 'case', product: 'ForeverPin' })]);
  assert.deepEqual(products.map((entry) => entry.runnerProduct), ['foreverpin', 'forever-pin', 'ForeverPin']);
});

test('repository mismatch leaves both the conflicted registry record and independent runner product', () => {
  const { products } = buildWorkspaceInventory([product({ repo: 'other/repository' })], [target()]);
  assert.equal(products[0].binding, 'conflict');
  assert.equal(products[0].issue, 'repository-mismatch');
  assert.equal(products[0].runnerProduct, null);
  assert.equal(products[1].key, 'runner:foreverpin');
});

test('ambiguous registry matches cannot claim a runner product', () => {
  const { products } = buildWorkspaceInventory([product(), product({ id: 'registry-2' })], [target()]);
  assert.deepEqual(products.slice(0, 2).map((entry) => entry.issue), ['ambiguous-registry', 'ambiguous-registry']);
  assert.equal(products[2].key, 'runner:foreverpin');
});

test('duplicate reviewed bindings fail closed even when identical', () => {
  const { products } = buildWorkspaceInventory([product()], [target()],
    [WorkspaceProductBindings[0], WorkspaceProductBindings[0]]);
  assert.equal(products[0].issue, 'duplicate-binding');
  assert.equal(products[1].key, 'runner:foreverpin');
});

test('two different registry bindings cannot claim the same runner product', () => {
  const bindings = [...WorkspaceProductBindings,
    { registrySlug: 'second', repository: 'owner/second', runnerProduct: 'foreverpin' }];
  const { products } = buildWorkspaceInventory([product(),
    product({ id: 'registry-2', slug: 'second', repo: 'owner/second' })], [target()], bindings);
  assert.deepEqual(products.slice(0, 2).map((entry) => entry.issue), ['duplicate-binding', 'duplicate-binding']);
  assert.equal(products[2].key, 'runner:foreverpin');
});

test('preserves registered products with no target and groups multiple literal environments', () => {
  const registered = product({ id: 'unconfigured', slug: 'another', repo: 'owner/another' });
  const production = target({ id: 'foreverpin-production', environment: 'production' });
  const { products } = buildWorkspaceInventory([registered], [target(), production]);
  assert.equal(products[0].key, 'registry:unconfigured');
  assert.deepEqual(products[0].targets, []);
  assert.deepEqual(products[1].targets.map((entry) => entry.id), ['foreverpin-rehearsal', 'foreverpin-production']);
});

test('keeps an approved association when no deployment target is provisioned', () => {
  const { products } = buildWorkspaceInventory([product()], []);
  assert.equal(products[0].binding, 'mapped');
  assert.equal(products[0].runnerProduct, 'foreverpin');
  assert.deepEqual(products[0].targets, []);
});

test('does not mutate registry records, targets, arrays or bindings', () => {
  const registry = Object.freeze([Object.freeze(product())]);
  const targets = Object.freeze([Object.freeze(target())]);
  const bindings = Object.freeze(WorkspaceProductBindings.map((item) => Object.freeze({ ...item })));
  const before = JSON.stringify({ registry, targets, bindings });
  buildWorkspaceInventory(registry, targets, bindings);
  assert.equal(JSON.stringify({ registry, targets, bindings }), before);
});
