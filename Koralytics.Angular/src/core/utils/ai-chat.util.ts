/**
 * Strips internal metadata tags (such as ##PLAYER_CONTEXT:4## or ##TAG:VALUE##) from AI chatbot responses
 * so they are never displayed in the frontend UI.
 */
export function cleanAiBotResponse(text: string): string {
  if (!text) return '';
  return text
    .replace(/##PLAYER_CONTEXT:[^#]*##/gi, '')
    .replace(/##[A-Z_]+(?::[^#]*)?##/gi, '')
    .replace(/\n{3,}/g, '\n\n')
    .trim();
}

/**
 * Converts Markdown text (tables, headings, bullet/numbered lists, bold, italics, inline code)
 * into clean, beautifully styled HTML that matches Langflow AI design.
 * Strips all '#' symbols from headings/text and filters out table alignment separator rows (| :--- |).
 */
export function formatAiMarkdown(text: string): string {
  if (!text) return '';

  const cleaned = cleanAiBotResponse(text).replace(/\r\n/g, '\n');
  const lines = cleaned.split('\n');
  const resultBlocks: string[] = [];

  let inTable = false;
  let tableHeader: string[] = [];
  let tableRows: string[][] = [];

  let inList = false;
  let listType: 'ul' | 'ol' = 'ul';
  let listItems: string[] = [];

  const flushTable = () => {
    if (!inTable) return;
    if (tableHeader.length > 0) {
      let tableHtml = '<div class="ai-table-wrapper"><table class="ai-markdown-table"><thead><tr>';
      tableHeader.forEach(h => {
        tableHtml += `<th>${formatInlineMarkdown(h)}</th>`;
      });
      tableHtml += '</tr></thead><tbody>';

      tableRows.forEach(row => {
        tableHtml += '<tr>';
        row.forEach(cell => {
          tableHtml += `<td>${formatInlineMarkdown(cell)}</td>`;
        });
        tableHtml += '</tr>';
      });

      tableHtml += '</tbody></table></div>';
      resultBlocks.push(tableHtml);
    }
    inTable = false;
    tableHeader = [];
    tableRows = [];
  };

  const flushList = () => {
    if (!inList) return;
    const tag = listType === 'ol' ? 'ol' : 'ul';
    const cssClass = listType === 'ol' ? 'ai-md-ol' : 'ai-md-ul';
    let listHtml = `<${tag} class="${cssClass}">`;
    listItems.forEach(item => {
      listHtml += `<li class="ai-md-li">${formatInlineMarkdown(item)}</li>`;
    });
    listHtml += `</${tag}>`;
    resultBlocks.push(listHtml);
    inList = false;
    listItems = [];
  };

  for (let i = 0; i < lines.length; i++) {
    const line = lines[i].trim();

    // Check table line: contains '|' and has at least 2 cells
    const isTableLine = line.includes('|') && line.split('|').length >= 3;

    if (isTableLine) {
      flushList();
      const cells = line
        .split('|')
        .map(c => c.trim())
        .filter((_, idx, arr) => idx > 0 && idx < arr.length - 1 || arr.length === 1);

      // Check if separator line (e.g. | :--- | :--- | or | --- | --- |)
      // A separator line is any line where ALL cells consist solely of '-', ':', and spaces
      const isSeparator = cells.length > 0 && cells.every(c => /^[:\-\s]+$/.test(c) && c.includes('-'));

      if (isSeparator) {
        continue; // Skip table header separator line completely
      }

      if (!inTable) {
        inTable = true;
        tableHeader = cells;
      } else {
        tableRows.push(cells);
      }
      continue;
    } else {
      flushTable();
    }

    // Check Headings (e.g. # Heading, ### Heading, ###Heading)
    if (/^#+/.test(line)) {
      flushList();
      const cleanHeadingLine = line.replace(/^#+\s*/, '').trim();
      if (cleanHeadingLine) {
        const hashCount = (line.match(/^#+/) || [''])[0].length;
        const level = Math.min(Math.max(hashCount, 1), 6);
        const headingText = formatInlineMarkdown(cleanHeadingLine);
        resultBlocks.push(`<h${level} class="ai-md-h${level}">${headingText}</h${level}>`);
      }
      continue;
    }

    // Check Horizontal Rule
    if (/^---+$|^\*\*\*+$|^___+$/.test(line)) {
      flushList();
      resultBlocks.push('<hr class="ai-md-hr" />');
      continue;
    }

    // Check Unordered List (- or *)
    const ulMatch = line.match(/^[\-\*]\s+(.*)$/);
    if (ulMatch) {
      if (inList && listType !== 'ul') {
        flushList();
      }
      inList = true;
      listType = 'ul';
      listItems.push(ulMatch[1]);
      continue;
    }

    // Check Ordered List (1. 2. etc)
    const olMatch = line.match(/^\d+\.\s+(.*)$/);
    if (olMatch) {
      if (inList && listType !== 'ol') {
        flushList();
      }
      inList = true;
      listType = 'ol';
      listItems.push(olMatch[1]);
      continue;
    }

    // Not list line
    flushList();

    if (line === '') {
      continue;
    }

    // Normal paragraph line
    resultBlocks.push(`<p class="ai-md-p">${formatInlineMarkdown(line)}</p>`);
  }

  flushTable();
  flushList();

  return resultBlocks.join('\n');
}

/**
 * Formats inline elements: **bold**, *italic*, `code`
 * Removes any remaining '#' symbols so none appear in the UI.
 */
function formatInlineMarkdown(text: string): string {
  if (!text) return '';

  let html = text
    .replace(/#/g, '') // Completely erase any '#' symbols from display
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;');

  // Inline code `code`
  html = html.replace(/`([^`]+)`/g, '<code class="ai-code-inline">$1</code>');

  // Bold **text**
  html = html.replace(/\*\*(.*?)\*\*/g, '<strong class="ai-bold-highlight">$1</strong>');

  // Italic *text*
  html = html.replace(/\*(.*?)\*/g, '<em>$1</em>');

  return html;
}


