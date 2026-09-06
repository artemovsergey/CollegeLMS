const { chromium } = require('playwright');

(async () => {
  const browser = await chromium.launch({ headless: false });
  const page = await browser.newPage({ viewport: { width: 1366, height: 768 } });

  // Login
  await page.goto('http://176.109.105.252/login');
  await page.fill('input[name="login"], input[type="text"]', 'dispatcher');
  await page.fill('input[name="password"], input[type="password"]', 'dispatcher123');
  await page.click('button[type="submit"]');
  await page.waitForURL('**/dispatcher/dashboard', { timeout: 10000 });

  // Go to schedule
  await page.goto('http://176.109.105.252/schedule');
  await page.waitForTimeout(2000);

  // Screenshot before click
  await page.screenshot({ path: 'C:/Users/asv/Desktop/CollegeLMS/before_click.png', fullPage: false });
  console.log('Screenshot 1: before click');

  // Get bounding box of the filter bar
  const filterBar = await page.$('.flex.items-center.gap-3.rounded-lg.border');
  if (filterBar) {
    const box1 = await filterBar.boundingBox();
    console.log('Filter bar BEFORE:', JSON.stringify(box1));
  }

  // Click on the group select trigger
  const selectTriggers = await page.$$('button[role="combobox"]');
  console.log('Found', selectTriggers.length, 'select triggers');

  if (selectTriggers.length > 0) {
    // Click first select (group)
    await selectTriggers[0].click();
    await page.waitForTimeout(500);

    // Screenshot after opening dropdown
    await page.screenshot({ path: 'C:/Users/asv/Desktop/CollegeLMS/after_open_dropdown.png', fullPage: false });
    console.log('Screenshot 2: after opening dropdown');

    // Check filter bar again
    if (filterBar) {
      const box2 = await filterBar.boundingBox();
      console.log('Filter bar AFTER open:', JSON.stringify(box2));
    }

    // Select first item
    const items = await page.$$('[role="option"]');
    if (items.length > 0) {
      await items[0].click();
      await page.waitForTimeout(1000);
    }

    // Screenshot after selection
    await page.screenshot({ path: 'C:/Users/asv/Desktop/CollegeLMS/after_select.png', fullPage: false });
    console.log('Screenshot 3: after selection');

    if (filterBar) {
      const box3 = await filterBar.boundingBox();
      console.log('Filter bar AFTER select:', JSON.stringify(box3));
    }
  }

  // Now check the entire page width at each step
  const pageWidth = await page.evaluate(() => document.documentElement.scrollWidth);
  const pageHeight = await page.evaluate(() => document.documentElement.scrollHeight);
  console.log(`Page dimensions: ${pageWidth}x${pageHeight}`);

  // Check all elements in the filter bar for width changes
  const filterChildren = await page.$$eval('.flex.items-center.gap-3.rounded-lg.border > *', (els) =>
    els.map(el => ({
      tag: el.tagName,
      class: el.className?.substring(0, 60),
      rect: el.getBoundingClientRect(),
    }))
  );
  console.log('Filter bar children:');
  filterChildren.forEach((c, i) => {
    console.log(`  ${i}: ${c.tag} w=${Math.round(c.rect.width)} h=${Math.round(c.rect.height)} x=${Math.round(c.rect.x)}`);
  });

  await browser.close();
})();
