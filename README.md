# HTTPDictionaryServer
Multithreading HTTP Server for Chinese dictionary website. Can be hooked up to a database or TSV file system. 
Supports 9 columns for word entries and 3 for sentence entries. 

Word columns in order:  
Chinese:  The Chinese word in traditional Chinese. Doubles as ID so must be unique.
Simplified: Chinese word in simplified Chinese.
Alternate: Alternate form of Chinese word (身分證, 身份證).
Zhuyin: Official Republic of China pronunciation in Taiwanese Zhuyin system.
Pinyin: Official Republic of China pronunciation in pinyin system. 
Mainland: Official People's Republic of China pronunciation in pinyin system.
measure:  Measure words associated with the word if applicable.
definition: English definitions.
frequency: Calculated automatically. The total number of sentences in which the word appears.

Sentence columns in order:
Chinese: The Chinese sentence in traditional Chinese.
English: The English translation of the sentence.
MouseOver: A string containing HTML used for mouseover popup tooltips for each word in the sentence. Generated automatically.

Algorithms:
Search: english word exact,phrase containing english word,Chinese word exact,containing chinese word, pinyin, zhuyin, sentence containing.
Result: Frequency, similarity to query, length

Performance:
With 550,000 word entries and 45,000 sentence entries it executes a search query using all search algorithms followed by result 
algorithms in around 200ms at 3.5Ghz. It utilizes multithreading to reach a capacity of around 30 requests per second.
