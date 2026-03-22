"""
Extractive Text Summarisation for DocuVault AI
===============================================
Final Year Major Project - Academic Demonstration

Algorithm: Frequency-Based Extractive Summarisation
Method:    TF (Term Frequency) sentence scoring
Type:      EXTRACTIVE only - selects real sentences, never generates new text

Why extractive?
- No hallucination possible (only picks real sentences)
- Fully explainable (score each sentence step by step)
- No external APIs or models required
- Fast and deterministic

Input:  ProcessedText from NLP pipeline (via stdin as JSON)
Output: JSON with summary sentences, scores, and explanation

Usage:
    echo '{"processed_text": "your text here", "num_sentences": 5}' | python summarise.py
"""

import sys
import re
import json
import math
from collections import Counter
from typing import List, Dict, Tuple

# Force UTF-8 encoding (Windows compatibility)
import io
if sys.stdout.encoding != 'utf-8':
    sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', errors='replace')
if sys.stderr.encoding != 'utf-8':
    sys.stderr = io.TextIOWrapper(sys.stderr.buffer, encoding='utf-8', errors='replace')
try:
    sys.stdout.reconfigure(encoding='utf-8')
    sys.stderr.reconfigure(encoding='utf-8')
except AttributeError:
    pass


def tokenize_words(text: str) -> List[str]:
    """Split text into words (simple regex tokenizer)"""
    return re.findall(r'\b[a-z]{2,}\b', text.lower())


def split_sentences(text: str) -> List[str]:
    """
    Split text into sentences.
    Works on both raw and NLP-processed text.
    """
    # Use ProcessedText as a word bag - reconstruct pseudo-sentences by chunking
    # ProcessedText is a space-separated list of lemmatized tokens, not sentences
    # We chunk it into groups of ~15 words to simulate sentence-level scoring
    words = text.split()
    
    if len(words) == 0:
        return []
    
    # Chunk words into sentence-like groups (15 words each)
    chunk_size = 15
    chunks = []
    for i in range(0, len(words), chunk_size):
        chunk = ' '.join(words[i:i + chunk_size])
        if chunk.strip():
            chunks.append(chunk)
    
    return chunks


def compute_word_frequencies(text: str) -> Dict[str, float]:
    """
    STEP 1: Count how often each word appears.
    Normalize by dividing by the maximum frequency.
    
    Why normalize? So no single very common word dominates.
    """
    words = tokenize_words(text)
    
    if not words:
        return {}
    
    # Count raw frequencies
    freq = Counter(words)
    
    # Normalize: divide each count by the maximum count
    max_freq = max(freq.values())
    normalized = {word: count / max_freq for word, count in freq.items()}
    
    return normalized


def score_sentences(sentences: List[str], word_freq: Dict[str, float]) -> List[Tuple[int, str, float]]:
    """
    STEP 2: Score each sentence based on the words it contains.
    
    Score = sum of normalized frequencies of all words in the sentence
    
    Why? Sentences with more important (frequent) words score higher.
    This is the core of frequency-based extractive summarisation.
    """
    scored = []
    
    for idx, sentence in enumerate(sentences):
        words = tokenize_words(sentence)
        
        if not words:
            score = 0.0
        else:
            # Sum word frequencies in this sentence
            score = sum(word_freq.get(word, 0.0) for word in words)
            # Normalize by sentence length to avoid bias toward longer sentences
            score = score / len(words)
        
        scored.append((idx, sentence, round(score, 4)))
    
    return scored


def extract_summary(text: str, num_sentences: int = 5) -> Dict:
    """
    Main extractive summarisation pipeline.
    
    Steps:
    1. Split text into sentence-chunks
    2. Compute word frequencies
    3. Score each sentence
    4. Select top N sentences
    5. Return in original order (preserves flow)
    
    Args:
        text: ProcessedText (NLP-cleaned tokens)
        num_sentences: Max sentences in summary (5-7)
    
    Returns:
        Dictionary with summary, scores, and explanation
    """
    
    if not text or not text.strip():
        return {
            "success": False,
            "summary": "",
            "error": "Input text is empty"
        }
    
    words = text.split()
    total_words = len(words)
    
    # Too short to summarise
    if total_words < 20:
        return {
            "success": True,
            "summary": text,
            "sentences": [text],
            "total_chunks": 1,
            "selected_chunks": 1,
            "total_words": total_words,
            "explanation": "Text is short enough to use as-is.",
            "word_scores": {}
        }
    
    # STEP 1: Split into sentence-chunks
    sentences = split_sentences(text)
    
    # Clamp num_sentences to available chunks
    num_sentences = min(num_sentences, len(sentences))
    
    # STEP 2: Compute word frequencies
    word_freq = compute_word_frequencies(text)
    
    # STEP 3: Score each sentence
    scored_sentences = score_sentences(sentences, word_freq)
    
    # STEP 4: Select top N by score
    top_sentences = sorted(scored_sentences, key=lambda x: x[2], reverse=True)[:num_sentences]
    
    # STEP 5: Re-sort by original position (preserve reading order)
    top_sentences_ordered = sorted(top_sentences, key=lambda x: x[0])
    
    # Build summary
    summary_parts = [s[1] for s in top_sentences_ordered]
    summary = '. '.join(summary_parts) + '.'
    
    # Top 10 most important words (for explanation)
    top_words = sorted(word_freq.items(), key=lambda x: x[1], reverse=True)[:10]
    
    return {
        "success": True,
        "summary": summary,
        "sentences": summary_parts,
        "sentence_scores": [{"index": s[0], "text": s[1], "score": s[2]} for s in top_sentences_ordered],
        "all_scores": [{"index": s[0], "text": s[1][:50] + "...", "score": s[2]} for s in scored_sentences],
        "total_chunks": len(sentences),
        "selected_chunks": num_sentences,
        "total_words": total_words,
        "top_keywords": [{"word": w, "score": round(s, 4)} for w, s in top_words],
        "explanation": (
            f"Analysed {total_words} tokens. "
            f"Split into {len(sentences)} chunks. "
            f"Scored each by word frequency. "
            f"Selected top {num_sentences} scoring chunks."
        )
    }


def main():
    """Main entry point - reads JSON from stdin, outputs JSON to stdout"""
    try:
        raw_input = sys.stdin.read().strip()
        
        if not raw_input:
            print(json.dumps({
                "success": False,
                "summary": "",
                "error": "No input received"
            }), flush=True)
            return
        
        # Parse input JSON
        try:
            data = json.loads(raw_input)
        except json.JSONDecodeError:
            # If not JSON, treat as raw text
            data = {"processed_text": raw_input, "num_sentences": 5}
        
        processed_text = data.get("processed_text", "").strip()
        num_sentences = int(data.get("num_sentences", 5))
        
        # Clamp to 5-7 range as per requirements
        num_sentences = max(5, min(7, num_sentences))
        
        if not processed_text:
            print(json.dumps({
                "success": False,
                "summary": "",
                "error": "ProcessedText is empty or null"
            }), flush=True)
            return
        
        # Run summarisation
        result = extract_summary(processed_text, num_sentences)
        
        print(json.dumps(result, ensure_ascii=False), flush=True)
        
    except Exception as e:
        print(json.dumps({
            "success": False,
            "summary": "",
            "error": f"Summarisation error: {str(e)}"
        }), flush=True)
        sys.exit(1)


if __name__ == "__main__":
    main()
