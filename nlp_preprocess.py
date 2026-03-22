"""
NLP Preprocessing Pipeline for DocuVault AI
============================================
Updated to match NlpService.cs JSON structure

Purpose: Preprocess OCR-extracted text for NLP/ML tasks
Input:  Raw OCR text (via stdin)
Output: JSON with processed text (via stdout)
"""

import sys
import re
import json
from typing import List, Dict, Optional

# Force UTF-8 encoding
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

# Import NLP libraries
try:
    import nltk
    from nltk.tokenize import sent_tokenize, word_tokenize
    from nltk.corpus import stopwords
    from nltk.stem import WordNetLemmatizer
except ImportError:
    print(json.dumps({
        "success": False,
        "processedText": "",
        "error": "NLTK not installed. Run: pip install nltk"
    }), flush=True)
    sys.exit(1)

# Download required NLTK data
def ensure_nltk_data():
    """Download required NLTK datasets if not present"""
    required_data = ['punkt', 'stopwords', 'wordnet', 'averaged_perceptron_tagger', 'omw-1.4']
    
    for dataset in required_data:
        try:
            nltk.data.find(f'tokenizers/{dataset}')
        except LookupError:
            try:
                nltk.data.find(f'corpora/{dataset}')
            except LookupError:
                try:
                    nltk.download(dataset, quiet=True)
                except:
                    pass

# Initialize NLP components
try:
    ensure_nltk_data()
    lemmatizer = WordNetLemmatizer()
    stop_words = set(stopwords.words('english'))
except Exception as e:
    print(json.dumps({
        "success": False,
        "processedText": "",
        "error": f"NLTK initialization failed: {str(e)}"
    }), flush=True)
    sys.exit(1)


def preprocess_text(text: str) -> Dict:
    """
    Main NLP preprocessing pipeline
    Returns JSON matching NlpService.cs expectations
    """
    
    if not text or not text.strip():
        return {
            "success": False,
            "processedText": "",
            "error": "Input text is empty"
        }
    
    try:
        # STEP 1: Lowercase conversion
        text_lower = text.lower()
        
        # STEP 2: Remove extra whitespace
        cleaned_text = re.sub(r'\s+', ' ', text_lower).strip()
        
        # STEP 3: Remove special characters (keep letters, numbers, basic punctuation)
        cleaned_text = re.sub(r'[^a-z0-9\s\.\,\?\!]', '', cleaned_text)
        
        # STEP 4: Sentence splitting
        try:
            sentences = sent_tokenize(cleaned_text)
        except:
            sentences = cleaned_text.split('.')
        
        # STEP 5: Tokenization (word-level)
        try:
            all_tokens = []
            for sentence in sentences:
                tokens = word_tokenize(sentence)
                all_tokens.extend(tokens)
        except:
            all_tokens = cleaned_text.split()
        
        # STEP 6: Remove stopwords
        filtered_tokens = [token for token in all_tokens if token not in stop_words and len(token) > 1]
        
        # STEP 7: Lemmatization
        lemmatized_tokens = []
        for token in filtered_tokens:
            try:
                lemma = lemmatizer.lemmatize(token)
                if len(lemma) >= 2:
                    lemmatized_tokens.append(lemma)
            except:
                if len(token) >= 2:
                    lemmatized_tokens.append(token)
        
        # Reconstruct processed text
        processed_text = ' '.join(lemmatized_tokens)
        
        # ✅ Return JSON matching NlpService.cs structure
        return {
            "success": True,
            "processedText": processed_text,
            "cleanedText": cleaned_text,
            "sentences": sentences,
            "tokens": all_tokens,
            "filteredTokens": filtered_tokens,
            "lemmatizedTokens": lemmatized_tokens,
            "stats": {
                "originalLength": len(text),
                "cleanedLength": len(cleaned_text),
                "numSentences": len(sentences),
                "numTokens": len(all_tokens),
                "numFilteredTokens": len(filtered_tokens),
                "numLemmatizedTokens": len(lemmatized_tokens)
            }
        }
        
    except Exception as e:
        return {
            "success": False,
            "processedText": "",
            "error": f"Preprocessing failed: {str(e)}"
        }


def main():
    """Main entry point"""
    try:
        # Read input from stdin
        input_text = sys.stdin.read()
        
        # Process text
        result = preprocess_text(input_text)
        
        # Output as JSON
        print(json.dumps(result, ensure_ascii=False), flush=True)
        
    except Exception as e:
        error_result = {
            "success": False,
            "processedText": "",
            "error": str(e)
        }
        print(json.dumps(error_result, ensure_ascii=False), flush=True)
        sys.exit(1)


if __name__ == "__main__":
    main()
