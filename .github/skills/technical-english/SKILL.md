---
name: technical-english
description: Rewrite or audit any text using the principles of ASD-STE100 Simplified Technical English, the aerospace writing standard built for flight manuals. Kills AI slop by force - short sentences, one approved meaning per word, active voice, no filler. Use when asked to "simplify this", "de-slop this", "make this clearer", "rewrite in plain English", "apply STE", or when documentation, instructions, README files, or AI-generated text need to be readable by every English speaker, including non-native ones.
argument-hint: <text, file path, or "audit" + text>
---

# Technical English (STE-inspired)

Aircraft maintenance manuals cannot afford ambiguity. A mechanic who misreads
one sentence can kill people. So the aerospace industry built ASD-STE100
Simplified Technical English: a controlled language where every word has one
meaning, every sentence has a length limit, and every instruction is a command.

AI-generated text fails in the exact opposite way: long sentences, hedged
claims, five-syllable words doing two-syllable jobs, and filler that sounds
like content. Applying STE principles to it is a mechanical de-slop pass.

This skill applies those principles. It is inspired by ASD-STE100 and is not
the official specification (see the attribution note at the end).

## The rules

Apply all of them. When two rules conflict, clarity wins.

### 1. One word, one meaning
Every word keeps its most common meaning and its most common part of speech.
"Follow" means "come after", never "obey". "Check" is a verb, not a noun.
If a word can be read two ways in context, replace it.

### 2. Sentence limits (count, do not guess)
- Instructions: **20 words maximum.**
- Descriptions: **25 words maximum.**
- Paragraphs: **6 sentences maximum**, one topic each.
Count the words of every sentence you output. A 21-word instruction is a
violation, not a style choice.

### 3. One instruction per sentence
"Remove the cover and disconnect the cable and check the seal" is three
instructions. Write three sentences. Sequences become numbered lists.

### 4. Active voice, imperative instructions
- Instruction: "Remove the bolt." Never "The bolt should be removed."
- Description: name the agent. "The pump moves the fuel", not "the fuel is
  moved."
Passive voice is permitted only when the agent is truly unknown or
irrelevant, and never in an instruction.

### 5. Present tense
Write what is, not what will be or would be. "The valve opens when pressure
reaches 30 psi." Use past or future only when the fact itself is past or
future.

### 6. Keep the articles
Do not drop "the", "a", "an" to save words. "Close valve" reads like a
telegram; "Close the valve" reads like an instruction.

### 7. Break noun clusters
More than three nouns in a row is a wall. "Main gearbox oil pressure warning
light" becomes "the warning light for the oil pressure of the main gearbox."

### 8. Warnings come first
Command first, then the condition: "Do not open the cover before you
disconnect the power." Never bury the danger at the end of the sentence.

### 9. The substitution table
Replace unapproved words with their plain equivalents:

| Never write | Write |
|---|---|
| utilize, leverage | use |
| commence, initiate | start |
| terminate | stop |
| accomplish, perform, execute | do |
| sufficient | enough |
| demonstrate, indicate | show |
| ensure | make sure |
| facilitate | help |
| prior to | before |
| subsequent to | after |
| in order to | to |
| additionally, furthermore, moreover | also |
| approximately | about |
| numerous | many |
| obtain | get |
| require | need |
| attempt | try |
| modification | change |

### 10. The AI-slop blocklist
Delete on sight, they carry no information: *delve, crucial, comprehensive,
seamless, robust, cutting-edge, game-changer, landscape, realm, tapestry,
unlock, elevate, foster, streamline, "it's important to note", "it's worth
mentioning", "in today's world", "at the end of the day"*. Also delete every
em dash; use a comma, a colon, or a full stop.

## How to run

**Rewrite mode (default).** Take the input text and:
1. Read the whole text first. Identify its purpose: instruction, description,
   or mixed.
2. List the violations you found (rule number + the offending words). Keep
   this list short and specific.
3. Rewrite the full text under the rules above. Preserve every fact. Never
   add a fact, a number, or a claim that the input does not contain.
4. Verify: recount sentence lengths, rescan for blocklist words and passive
   instructions. Fix what you find.
5. Output the rewrite first, then the violation list under a `## What changed`
   heading.

**Audit mode.** When asked to audit (not rewrite), output only the violation
report: rule number, quoted offending text, suggested fix, one line each.

**Light mode.** For marketing copy, posts, or anything with a voice worth
keeping, apply only rules 2, 4, 9, and 10, and say you did. Full STE strips
personality by design; do not apply it to text that needs one unless asked.

## What this skill refuses to do

- It does not shorten by deleting facts. If the input is 40 facts, the output
  is 40 facts in shorter sentences.
- It does not simplify quoted text, code, commands, or error messages. Those
  are exact strings; leave them exact.
- It does not guess a missing fact to complete a sentence. It marks the gap:
  "[value missing]".

## Example

**Before (typical AI output, 44-word sentence):**
> In order to facilitate the seamless integration of the authentication
> module, it is important to note that developers should ensure that the
> configuration file has been comprehensively updated prior to commencing
> the deployment process, which can be accomplished via the CLI.

**After (STE principles, longest sentence 11 words):**
> Update the configuration file before you start the deployment. Use the
> CLI to deploy. This connects the authentication module.

Same facts. A third of the words. No ambiguity.

---

**Attribution:** Inspired by ASD-STE100 Simplified Technical English, a
specification maintained by the AeroSpace and Defence Industries Association
of Europe. This skill applies its principles and is not the official
specification or its dictionary. The official specification is available
free from asd-ste100.org.
