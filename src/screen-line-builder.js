const Point = require('./point')
const {traverse, traversal, compare, max, isEqual} = require('./point-helpers')

const HARD_TAB = 1 << 0
const LEADING_WHITESPACE = 1 << 2
const TRAILING_WHITESPACE = 1 << 3
const INVISIBLE_CHARACTER = 1 << 4
const INDENT_GUIDE = 1 << 5
const LINE_ENDING = 1 << 6
const FOLD = 1 << 7

const basicTagCache = new Map()
let nextScreenLineId = 1

module.exports =
class ScreenLineBuilder {
  constructor (displayLayer) {
    this.displayLayer = displayLayer
  }

  buildScreenLines (screenStartRow, screenEndRow) {
    screenEndRow = Math.min(screenEndRow, this.displayLayer.getScreenLineCount())
    const screenStart = Point(screenStartRow, 0)
    const screenEnd = Point(screenEndRow, 0)
    let screenLines = []
    let screenRow = screenStartRow
    let {row: bufferRow} = this.displayLayer.translateScreenPosition(screenStart)

    let hunkIndex = 0
    const hunks = this.displayLayer.spatialIndex.getHunksInNewRange(screenStart, screenEnd)
    while (screenRow < screenEndRow) {
      let screenLineText = ''
      let tagCodes = []
      let currentTokenLength = 0
      let currentTokenFlags = 0
      let screenColumn = 0
      let bufferLine = this.displayLayer.buffer.lineForRow(bufferRow)
      let lineEnding = this.displayLayer.buffer.lineEndingForRow(bufferRow)
      let bufferColumn = 0
      let trailingWhitespaceStartColumn = this.displayLayer.findTrailingWhitespaceStartColumn(bufferLine)
      let inLeadingWhitespace = true
      let inTrailingWhitespace = false

      // If the buffer line is empty, indent guides may extend beyond the line-ending
      // invisible, requiring this separate code path.
      while (bufferColumn <= bufferLine.length) {
        let previousTokenFlags = currentTokenFlags
        currentTokenFlags = 0

        // Handle folds or soft wraps at the current position.
        let nextHunk = hunks[hunkIndex]
        while (nextHunk && nextHunk.oldStart.row < bufferRow) {
          hunkIndex++
          nextHunk = hunks[hunkIndex]
        }

        while (nextHunk && nextHunk.oldStart.row === bufferRow && nextHunk.oldStart.column === bufferColumn) {
          // Does a fold hunk start here? Jump to the end of the fold and
          // continue to the next iteration of the loop.
          if (nextHunk.newText === this.displayLayer.foldCharacter) {
            if (previousTokenFlags > 0) {
              this.pushCloseTag(tagCodes, currentTokenLength, this.getBasicTag(previousTokenFlags))
            } else if (currentTokenLength > 0) {
              tagCodes.push(currentTokenLength)
            }
            currentTokenLength = 0

            screenLineText += this.displayLayer.foldCharacter
            screenColumn++
            this.pushOpenTag(tagCodes, currentTokenLength, this.getBasicTag(FOLD))
            previousTokenFlags = FOLD
            currentTokenLength = this.displayLayer.foldCharacter.length
            bufferRow = nextHunk.oldEnd.row
            bufferColumn = nextHunk.oldEnd.column
            bufferLine = this.displayLayer.buffer.lineForRow(bufferRow)
            inTrailingWhitespace = false
            trailingWhitespaceStartColumn = this.displayLayer.findTrailingWhitespaceStartColumn(bufferLine)

          // If the oldExtent of the hunk is zero, this is a soft line break.
          } else if (isEqual(nextHunk.oldStart, nextHunk.oldEnd)) {
            if (previousTokenFlags > 0) {
              this.pushCloseTag(tagCodes, currentTokenLength, this.getBasicTag(previousTokenFlags))
              previousTokenFlags = 0
            } else if (currentTokenLength > 0) {
              tagCodes.push(currentTokenLength)
            }
            currentTokenLength = 0

            const screenLine = {id: nextScreenLineId++, lineText: screenLineText, tagCodes}
            screenLines.push(screenLine)
            screenRow++

            // Make indent of soft-wrapped segment match the indent of the
            // original line, rendering indent guides if necessary.
            const indentLength = nextHunk.newEnd.column
            screenLineText = ' '.repeat(indentLength)
            tagCodes = []
            if (this.displayLayer.showIndentGuides && indentLength > 0) {
              screenColumn = 0
              while (screenColumn < indentLength) {
                if (screenColumn % this.displayLayer.tabLength === 0) {
                  if (currentTokenLength > 0) {
                    tagCodes.push(currentTokenLength)
                    tagCodes.push(this.displayLayer.codeForCloseTag(this.getBasicTag(INDENT_GUIDE)))
                    currentTokenLength = 0
                  }
                  this.pushOpenTag(tagCodes, 0, this.getBasicTag(INDENT_GUIDE))
                }
                screenColumn++
                currentTokenLength++
              }
              if (currentTokenLength > 0) {
                tagCodes.push(currentTokenLength)
                tagCodes.push(this.displayLayer.codeForCloseTag(this.getBasicTag(INDENT_GUIDE)))
                currentTokenLength = 0
              }
            } else {
              screenColumn = indentLength
              currentTokenLength = indentLength
            }
          }

          hunkIndex++
          nextHunk = hunks[hunkIndex]
        }

        let forceTokenBoundary = false
        const nextCharacter = bufferLine[bufferColumn]
        if (bufferColumn >= trailingWhitespaceStartColumn) {
          inTrailingWhitespace = true
          inLeadingWhitespace = false
        }

        // Compute the flags for the current token describing how it should be
        // decorated. If these flags differ from the previous token flags, emit
        // a close tag for those flags. Also emit a close tag at a forced token
        // boundary, such as between two hard tabs or where we want to show
        // an indent guide between spaces.
        if (nextCharacter === ' ' || nextCharacter === '\t') {
          const showIndentGuides = this.displayLayer.showIndentGuides && (inLeadingWhitespace || trailingWhitespaceStartColumn === 0)
          if (inLeadingWhitespace) currentTokenFlags |= LEADING_WHITESPACE
          if (inTrailingWhitespace) currentTokenFlags |= TRAILING_WHITESPACE

          if (nextCharacter === ' ') {
            if ((inLeadingWhitespace || inTrailingWhitespace) && this.displayLayer.invisibles.space) {
              currentTokenFlags |= INVISIBLE_CHARACTER
            }

            if (showIndentGuides) {
              currentTokenFlags |= INDENT_GUIDE
              if (screenColumn % this.displayLayer.tabLength === 0) forceTokenBoundary = true
            }
          } else { // nextCharacter === \t
            currentTokenFlags |= HARD_TAB
            if (this.displayLayer.invisibles.tab) currentTokenFlags |= INVISIBLE_CHARACTER
            if (showIndentGuides && screenColumn % this.displayLayer.tabLength === 0) {
              currentTokenFlags |= INDENT_GUIDE
            }

            forceTokenBoundary = true
          }
        } else {
          inLeadingWhitespace = false
        }

        if (previousTokenFlags > 0 &&
            (currentTokenFlags !== previousTokenFlags || forceTokenBoundary)) {
          this.pushCloseTag(tagCodes, currentTokenLength, this.getBasicTag(previousTokenFlags))
          currentTokenLength = 0
        }

        // We loop up to the end of the buffer line in case a fold starts there,
        // but at this point we haven't found a fold, so we can stop if we have
        // reached the end of the line. We need to close any open tags and
        // append the line ending invisible if it is enabled, then break the
        // loop to proceed to the next line. If the line is empty, we may need
        // to render indent guides that extend beyond the length of the line.
        if (bufferColumn === bufferLine.length) {
          if (currentTokenLength > 0) {
            if (previousTokenFlags > 0) {
              this.pushCloseTag(tagCodes, currentTokenLength, this.getBasicTag(previousTokenFlags))
            } else {
              tagCodes.push(currentTokenLength)
            }
            currentTokenLength = 0
          }

          const eolInvisible = this.displayLayer.eolInvisibles[lineEnding]
          if (eolInvisible) {
            screenLineText += eolInvisible
            currentTokenFlags |= INVISIBLE_CHARACTER | LINE_ENDING
            if (bufferLine.length === 0 && this.displayLayer.showIndentGuides) currentTokenFlags |= INDENT_GUIDE
            this.pushOpenTag(tagCodes, 0, this.getBasicTag(currentTokenFlags))
            this.pushCloseTag(tagCodes, eolInvisible.length, this.getBasicTag(currentTokenFlags))
            screenColumn += eolInvisible.length
          }

          if (bufferLine.length === 0 && this.displayLayer.showIndentGuides) {
            currentTokenFlags = 0
            currentTokenLength = 0
            let whitespaceLength = this.displayLayer.leadingWhitespaceLengthForSurroundingLines(bufferRow)
            while (screenColumn < whitespaceLength) {
              if (screenColumn % this.displayLayer.tabLength === 0) {
                if (currentTokenLength > 0) {
                  tagCodes.push(currentTokenLength)
                }

                if (currentTokenFlags !== 0) {
                  tagCodes.push(this.displayLayer.codeForCloseTag(this.getBasicTag(currentTokenFlags)))
                }

                currentTokenLength = 0
                currentTokenFlags = INDENT_GUIDE
                this.pushOpenTag(tagCodes, 0, this.getBasicTag(currentTokenFlags))
              }
              screenLineText += ' '
              screenColumn++
              currentTokenLength++
            }
            if (currentTokenLength > 0) tagCodes.push(currentTokenLength)
            if (currentTokenFlags) tagCodes.push(this.displayLayer.codeForCloseTag(this.getBasicTag(currentTokenFlags)))
          }

          // Ensure empty lines have at least one empty token to make it easier on
          // the caller
          if (tagCodes.length === 0) tagCodes.push(0)

          break
        }

        // At this point we know we aren't at the end of the line, so we proceed
        // to process the next character.

        // If the current token's flags differ from the previous iteration or
        // we are forcing a token boundary (for example between two hard tabs),
        // push an open tag based on the new flags.
        if (currentTokenFlags > 0 &&
            currentTokenFlags !== previousTokenFlags || forceTokenBoundary) {
          this.pushOpenTag(tagCodes, currentTokenLength, this.getBasicTag(currentTokenFlags))
          currentTokenLength = 0
        }

        // Handle tabs and leading / trailing whitespace invisibles specially.
        // Otherwise just append the next character to the screen line.
        if (nextCharacter === '\t') {
          currentTokenLength = 0
          const distanceToNextTabStop = this.displayLayer.tabLength - (screenColumn % this.displayLayer.tabLength)
          if (this.displayLayer.invisibles.tab) {
            screenLineText += this.displayLayer.invisibles.tab
            screenLineText += ' '.repeat(distanceToNextTabStop - 1)
          } else {
            screenLineText += ' '.repeat(distanceToNextTabStop)
          }

          screenColumn += distanceToNextTabStop
          currentTokenLength += distanceToNextTabStop
        } else {
          if ((inLeadingWhitespace || inTrailingWhitespace) &&
              nextCharacter === ' ' && this.displayLayer.invisibles.space) {
            screenLineText += this.displayLayer.invisibles.space
          } else {
            screenLineText += nextCharacter
          }
          screenColumn++
          currentTokenLength++
        }
        bufferColumn++
      }

      const screenLine = {id: nextScreenLineId++, lineText: screenLineText, tagCodes}
      screenLines.push(screenLine)
      screenRow++
      bufferRow++
    }

    return screenLines
  }

  getBasicTag (flags) {
    let tag = basicTagCache.get(flags)
    if (tag) {
      return tag
    } else {
      let tag = ''
      if (flags & INVISIBLE_CHARACTER) tag += 'invisible-character '
      if (flags & HARD_TAB) tag += 'hard-tab '
      if (flags & LEADING_WHITESPACE) tag += 'leading-whitespace '
      if (flags & TRAILING_WHITESPACE) tag += 'trailing-whitespace '
      if (flags & LINE_ENDING) tag += 'eol '
      if (flags & INDENT_GUIDE) tag += 'indent-guide '
      if (flags & FOLD) tag += 'fold-marker '
      tag = tag.trim()
      basicTagCache.set(flags, tag)
      return tag
    }
  }

  pushCloseTag (tagCodes, currentTokenLength, closeTag) {
    if (currentTokenLength > 0) tagCodes.push(currentTokenLength)
    tagCodes.push(this.displayLayer.codeForCloseTag(closeTag))
  }

  pushOpenTag (tagCodes, currentTokenLength, openTag) {
    if (currentTokenLength > 0) tagCodes.push(currentTokenLength)
    tagCodes.push(this.displayLayer.codeForOpenTag(openTag))
  }
}